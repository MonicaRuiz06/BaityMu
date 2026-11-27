# Implementación de BaityMu

Este documento describe cómo quedó construida la solución: su arquitectura, las decisiones de diseño, los endpoints, las pruebas, los resultados del escenario de referencia y el front. Complementa a [Historias de usuario](HistoriasDeUsuario.md) (qué se pidió) y a [Base de datos](BaseDeDatos.md) (cómo se guarda).

## Arquitectura implementada

### Proyectos y dependencias

La solución `BaityMu.slnx` tiene los cuatro proyectos de capas que pide el taller, un proyecto de pruebas y un cliente web opcional.

| Proyecto | Responsabilidad | Contenido principal |
|---|---|---|
| `BaityMu.Domain` | Núcleo del negocio, sin dependencias externas | Entidades (`Granja`, `Vaca`, `ProduccionLeche`, `ParametroProducto`, `LoteProducto`, `Venta`, `VentaDetalle`), enums (`TipoProducto`, `EstadoLote`), excepciones de dominio e interfaces (`IRepository<T>`, repositorios, `IUnitOfWork`, `IClock`) |
| `BaityMu.Infrastructure` | Acceso a datos y servicios técnicos | `BaityMuDbContext` (EF Core + SQL Server), configuraciones de entidades, 5 migraciones, repositorios, `UnitOfWork`, `SystemClock` y el sembrado inicial de la granja |
| `BaityMu.Services` | Casos de uso y orquestación | `GranjaService`, `ProduccionService`, `InventarioService`, `VentaService`, `ReporteService`, DTOs de entrada y salida, modelos de lectura y `CsvExporter` |
| `BaityMu.Web` | Exposición HTTP y composición | 7 controladores, `Program.cs` (inyección de dependencias), manejador global de errores, Swagger, AutoMapper y CORS |
| `BaityMu.Tests` | Pruebas unitarias | 151 pruebas NUnit con `FakeClock` y EF Core InMemory |
| `BaityMu.Client` | Front (opcional) | Blazor WebAssembly que consume la API por HTTP |

```text
BaityMu.Web → BaityMu.Services → BaityMu.Domain
     └──────→ BaityMu.Infrastructure → BaityMu.Domain
```

- **Domain no conoce a ninguna otra capa.** Infrastructure implementa sus interfaces.
- **Web referencia a Infrastructure solo en `Program.cs`**, para registrar las dependencias. Ningún controlador usa el `DbContext`.
- **Los controladores no tienen lógica de negocio.** Reciben la petición, llaman un servicio y devuelven el resultado mapeado a un DTO.
- **El cliente Blazor no referencia ninguna capa.** Solo conoce la API por HTTP.

### Flujo de una petición

```text
Petición:   HTTP → Controller → Service → Dominio (reglas) → Repositorio → IUnitOfWork
Respuesta:  HTTP ← Controller ← DTO (AutoMapper) ← entidad o modelo de lectura
Error:      excepción de dominio → ManejadorGlobalDeErrores → ProblemDetails (400 / 404 / 409)
```

## Decisiones de diseño

### Las reglas viven en el dominio

Las entidades tienen setters privados y exponen métodos con las reglas, como pide el esqueleto de `LoteProducto` del taller. Un servicio nunca cambia una propiedad directamente.

| Regla | Dónde vive |
|---|---|
| Producción n × l, una vez por vaca y fecha (RN-01) | `Granja.GenerarProduccionDiaria` + índice único `(VacaId, Fecha)` |
| Transformación sin pasar lo producido (RN-02) y cantidad = litros ÷ rendimiento (RN-03) | `LoteProducto.Crear` y `ParametroProducto.CalcularCantidad` |
| Lote con vencimiento = producción + vida útil (RN-04) | `ParametroProducto.CalcularVencimiento`; la fecha queda fija al crear el lote |
| Venta FIFO, varios lotes por venta (RN-05, RN-06) | `Venta.Registrar` |
| Venta atómica (RN-07) | `Venta.Registrar` valida todo **antes** de descontar; el servicio guarda con un solo `SaveChangesAsync` |
| Vencido cuando fecha > vencimiento; pasa a dañado (RN-08) | `LoteProducto.EstaVencido` y `LoteProducto.MarcarComoDanado` |
| Trazabilidad de la venta (RN-09) | `VentaDetalle` por cada lote usado; el precio se copia al momento de vender |
| Fecha explícita opcional (RN-10) | Todas las operaciones reciben `fecha?`; si no llega, se usa `IClock.Today` |

### Venta FIFO paso a paso

`VentaService.RegistrarVentaAsync` sigue el procedimiento de la sección 7 del taller:

1. Toma la fecha recibida o la de `IClock`.
2. Pide al repositorio los lotes vendibles del producto (existencias > 0 y no vencidos a esa fecha), ordenados por fecha de producción.
3. `Venta.Registrar` suma las existencias. Si no alcanzan, lanza `InventarioInsuficienteException` (409) **sin tocar ningún lote**.
4. Recorre los lotes en orden, descuenta `min(disponible, restante)` de cada uno y crea un `VentaDetalle`.
5. El servicio agrega la venta y guarda la venta y los lotes en **una sola transacción** (`IUnitOfWork`).

### El estado del lote se calcula, no se guarda

`EstadoLote` (Disponible, Agotado, Vencido, Dañado) se calcula con `LoteProducto.ObtenerEstado(fecha)`. Por eso el inventario y los reportes aceptan `?fecha=`: el mismo lote aparece *Disponible* el 16 de diciembre y *Vencido* el 17, sin escribir nada en la base. Solo al **procesar vencimientos** lo disponible se mueve a la columna dañada, y procesar dos veces la misma fecha no duplica nada.

### Manejo centralizado de errores

El dominio comunica los errores de negocio con excepciones propias. Un solo manejador global (`ManejadorGlobalDeErrores`, registrado con `AddExceptionHandler` y `UseExceptionHandler`) las traduce a respuestas **ProblemDetails**. Por eso los controladores no tienen `try/catch`.

| Excepción | Código |
|---|---|
| `ReglaDeNegocioException` | 400 Bad Request |
| `RecursoNoEncontradoException` | 404 Not Found |
| `ConflictoException` (`ProduccionDuplicadaException`, `LecheInsuficienteException`, `InventarioInsuficienteException`) | 409 Conflict |
| Cualquier otra | 500, sin detalles internos |

### Configuración fuera del código

Los valores sugeridos del taller (n = 10, l = 15 L, rendimientos, vida útil y precios) están en `appsettings.json` (sección `GranjaInicial`). La primera vez que arranca, la API crea la granja con esos valores. Después se cambian con `PUT /api/granja`, sin tocar el código.

### Reportes

- `ReporteService` arma cada reporte como un DTO (son resúmenes calculados, no entidades) y valida el rango de fechas en un solo lugar.
- **Balance (producto total):** lo *disponible* excluye los lotes vencidos en la fecha de consulta. Lo vencido que todavía no se procesó sale aparte como `vencidoSinProcesar`. Así siempre se cumple: producido = vendido + disponible + vencido sin procesar + dañado.
- **Vacas:** se calculan dos promedios. El **promedio diario del hato** es el total ÷ los días con ordeño. El **promedio por vaca** es el total ÷ los ordeños, y es contra el que se compara cada vaca para saber si está por debajo y cuánto le falta.

## Endpoints de la API

Todas las fechas usan formato ISO (`yyyy-MM-dd`). Todas las respuestas son JSON, salvo los reportes pedidos con `formato=csv`.

### Operaciones

| Método | Ruta | Descripción | Respuestas |
|---|---|---|---|
| GET | `/api/granja` | Configuración: n, l, n × l y parámetros de cada producto | 200 |
| PUT | `/api/granja` | Actualiza l y el rendimiento, la vida útil y el precio de cada producto | 200, 400 |
| GET | `/api/vacas?incluirRetiradas=` | Lista las vacas con los litros que ha dado cada una | 200 |
| GET | `/api/vacas/{id}` | Una vaca con su historial de producción | 200, 404 |
| POST | `/api/vacas` | Registra una vaca (sin nombre: BaityCow, BaityCow 2…) | 201, 400, 404, 409 |
| DELETE | `/api/vacas/{id}` | Retira una vaca; conserva su historial | 204, 400, 404, 409 |
| POST | `/api/produccion` | Registra la producción de un día (n × l o litros por vaca) | 201, 400, 404, 409 |
| GET | `/api/produccion/{fecha}` | Producción de un día: total, transformado y disponible | 200, 404 |
| POST | `/api/transformaciones` | Transforma leche de un día en un lote de queso o yogurt | 201, 400, 409 |
| GET | `/api/inventario?tipoProducto=&desde=&hasta=&fecha=` | Lotes con su estado, en orden FIFO | 200, 400 |
| GET | `/api/inventario/lotes/{id}` | Un lote con su estado | 200, 404 |
| GET | `/api/inventario/resumen?fecha=` | Por producto: vendible, vencido sin procesar y dañado | 200 |
| GET | `/api/inventario/proximos-a-vencer?dias=3&fecha=` | Lotes con existencias que vencen pronto (extensión) | 200, 400 |
| POST | `/api/inventario/procesar-vencimientos?fecha=` | Lo vencido pasa a dañado; repetirlo no duplica | 200 |
| POST | `/api/ventas` | Venta FIFO con el detalle de los lotes usados | 201, 400, 409 |
| GET | `/api/ventas?desde=&hasta=&tipo=` | Lista las ventas con sus detalles | 200, 400 |
| GET | `/api/ventas/{id}` | Una venta con los lotes de donde salió | 200, 404 |

### Reportes

| Método | Ruta | Contenido |
|---|---|---|
| GET | `/api/reportes/ventas?desde=&hasta=&tipo=` | Cada venta (fecha, producto, cantidad, precio, total), total por producto y total general |
| GET | `/api/reportes/leche-diaria?desde=&hasta=` | Por día: litros producidos, transformados y no aprovechados |
| GET | `/api/reportes/producto-diario?desde=&hasta=` | Por día y producto: cantidad elaborada, unidad y litros usados |
| GET | `/api/reportes/producto-total?fecha=` | Por producto: producido, vendido, disponible, vencido sin procesar y dañado |
| GET | `/api/reportes/producto-danado?desde=&hasta=&tipo=` | Cada lote dañado con su fecha de vencimiento, y el total por producto |
| GET | `/api/reportes/vacas?desde=&hasta=` | Promedio del hato y por vaca, la que más y la que menos produce, y las que están por debajo del promedio con su faltante |

Todos los reportes responden 400 si `desde` es posterior a `hasta`, y 200 con listas vacías y totales en 0 si no hay datos. Con `&formato=csv` devuelven un archivo `.csv` (extensión opcional).

## Pruebas unitarias

El proyecto `BaityMu.Tests` tiene **151 pruebas NUnit**, todas en verde. Usan `FakeClock` para simular los días del escenario y EF Core InMemory para probar los servicios con sus repositorios reales.

### Pruebas mínimas del taller

| Prueba mínima (sección 9) | Prueba que la cubre |
|---|---|
| La producción diaria es n × l y no se registra dos veces la misma fecha | `GenerarProduccionDiaria_DiezVacasDeQuinceLitros_Genera150Litros` y `RegistrarAsync_MismaFechaDosVeces_LanzaDuplicadaYNoGuardaNada` |
| No se transforman más litros que los disponibles del día | `Crear_MasLitrosDeLosDisponiblesEseDia_LanzaLecheInsuficiente` y `TransformarAsync_MasDeLoQueQueda_LanzaLecheInsuficienteYNoCreaLote` |
| La venta toma del lote más antiguo antes que del más nuevo | `Registrar_TomaDelLoteMasAntiguoAntesQueDelMasNuevo` |
| Una venta que excede un lote consume el siguiente y genera varios detalles | `Registrar_QueExcedeUnLote_ConsumeElSiguienteYGeneraVariosDetalles` |
| Un lote vencido nunca se vende, aunque sea el más antiguo | `Registrar_UnLoteVencidoNuncaSeVende_AunqueSeaElMasAntiguo` |
| Una venta con existencias insuficientes no modifica el inventario | `Registrar_ConExistenciasInsuficientes_NoModificaElInventario` y `VentaRechazada_NoModificaNingunLoteEnLaBaseDeDatos` |
| Al procesar vencimientos lo disponible pasa a dañado y el reporte lo refleja | `MarcarComoDanado_LoteVencido_PasaLoDisponibleADanado`, `ReporteProductoDanado_DespuesDeProcesarElDia17_ApareceY1Con30L` y `EscenarioCompleto_Dias1Al17` |

### Organización

| Archivo | Qué prueba |
|---|---|
| `GranjaTests`, `ConfiguracionGranjaTests`, `ProduccionDiariaTests`, `ProduccionLecheTests`, `LoteProductoTests`, `VentaTests` | Reglas del dominio, sin base de datos |
| `GranjaServiceTests`, `ProduccionServiceTests`, `TransformacionServiceTests`, `InventarioServiceTests` | Casos de uso con repositorios reales sobre InMemory |
| `EscenarioDeReferenciaTests` | El escenario del taller de punta a punta (días 1 a 17), ventas, reportes y extensiones |
| `SembradorDeDatosTests`, `UnitOfWorkTests`, `FakeClockTests`, `CsvExporterTests` | Infraestructura y utilidades |

## Resultados del escenario de referencia

Con n = 10 vacas, l = 15 L, queso 10 L/kg y 30 días, y yogurt 1 L/L y 15 días. Se comprobó en las pruebas y en vivo contra SQL Server.

| Día | Operación | Resultado obtenido |
|---|---|---|
| 1 | Producción y transformación de 100 L en queso y 50 L en yogurt | 150 L; Q1 = 10 kg; Y1 = 50 L, vence el 16 de diciembre |
| 2 | Producción y transformación de 150 L en queso | 150 L; Q2 = 15 kg |
| 3 | Venta de 12 kg de queso | 10 kg de Q1 (agotado) + 2 kg de Q2 (quedan 13 kg); total $336.000 |
| 3 | Venta de 20 L de yogurt | Salen de Y1; quedan 30 L |
| 17 | Procesar vencimientos | Y1: 30 L pasan a dañado |
| 17 | Venta de 5 L de yogurt | 409 Conflict; no se modifica ningún lote |

Reporte de producto total al día 17:

| Producto | Producido | Vendido | Disponible | Dañado |
|---|---|---|---|---|
| Queso (kg) | 25 | 12 | 13 | 0 |
| Yogurt (L) | 50 | 20 | 0 | 30 |

El reporte de leche por día muestra 150 L producidos el día 1 y el día 2, con 0 L no aprovechados.

## Extensiones opcionales

| Extensión (sección 12) | Estado | Cómo |
|---|---|---|
| Alerta de lotes próximos a vencer | Hecha (HU-12) | `GET /api/inventario/proximos-a-vencer?dias=3`: lotes con existencias, no vencidos, que vencen dentro del plazo. En el front aparece como aviso en Inventario |
| Exportación de reportes a CSV o Excel | Hecha (HU-21) | `&formato=csv` en los seis reportes. `CsvExporter` usa `;` como separador, coma decimal y UTF-8 con BOM para que Excel en español lo abra directamente |
| Paginación y filtros avanzados | Parcial | Hay filtros por fechas y producto en el inventario, las ventas y los reportes; no hay paginación |

## Front (cliente web)

El taller no pide front. Lo agregué para poder demostrar el sistema de punta a punta sin depender solo de Swagger.

**El front lo construyó Claude (el asistente de IA de Anthropic) a partir de mis indicaciones, y lo conectó con el backend que desarrollé.** La API (dominio, infraestructura, servicios, controladores y pruebas) es mi trabajo. Los commits del front aparecen en el repositorio con Claude como coautor.

### Qué quedó

- **Blazor WebAssembly (.NET 10)** en el proyecto `BaityMu.Client`, que corre en el navegador en `http://localhost:5282`.
- **Cliente HTTP tipado** (`BaityMuApi`): un método por endpoint. Traduce los ProblemDetails de la API a mensajes para el usuario.
- **Día de trabajo** global en la barra superior, guardado en el navegador. Se envía como `fecha` a la API para simular los días del escenario (RN-10).
- **Pantallas por rol:** Inicio, Vacas (con ficha), Producción, Transformación, Inventario, Ventas, Reportes (6 pestañas con descarga CSV) y Configuración.
- **La API tiene CORS habilitado** (`AllowAnyOrigin`, como en el ejemplo del docente) para que el navegador la pueda llamar desde el puerto del front. El front no duplica reglas de negocio: la validación final siempre la hace la API.

### Ejemplos de las indicaciones (prompts) que le di a la IA

> **1. Arquitectura del cliente.** "Crea un cliente Blazor WebAssembly en .NET 10 como proyecto independiente (`BaityMu.Client`) que consuma la API REST existente sin referenciar ninguna de sus capas. Centraliza el acceso HTTP en un cliente tipado con un método por endpoint, serializa en camelCase con los enums como texto y traduce las respuestas ProblemDetails (400, 404, 409) a una excepción propia para mostrar el título y el detalle al usuario. La URL base de la API debe ir en `wwwroot/appsettings.json`, no quemada en el código. En la API, habilita CORS para que el navegador pueda llamarla desde el puerto del front."

> **2. Estado global y simulación de fechas.** "Necesito un 'día de trabajo' global, con un selector de fecha en la barra superior y un atajo para saltar al día 1 del escenario de referencia. Debe persistir en `localStorage`, notificar a las páginas cuando cambie para que recarguen sus datos y enviarse como el parámetro `fecha` en las peticiones, para poder recorrer los días 1 a 17 del taller sin cambiar el reloj del sistema."

> **3. Sistema de diseño y layout.** "Define un sistema de diseño con variables CSS (colores, tipografía, radios, sombras y espaciado) con estilo de 'libreta de granja'. Usa un layout con menú lateral agrupado por los roles del proyecto (BaityGanadero, BaityQuesero, BaityBodeguero, BaityVendedor, BaityGerente, BaityAdministrador), responsive, con menú desplegable en móvil. Crea componentes reutilizables para métricas, insignias de estado de lote, estados de carga, vacío y error, y notificaciones tipo toast. Usa la cultura es-CO para fechas, cantidades y pesos, y cuida la accesibilidad con roles ARIA en pestañas y gráficos."

> **4. Pantallas por caso de uso.** "Crea una página por grupo de historias de usuario, con el rol que las usa. En Ventas: selector de producto que muestre lo vendible, vista previa del total, aviso si la cantidad supera las existencias no vencidas y, al registrar, el detalle FIFO de cada lote usado. No dupliques reglas de negocio en el cliente: la vista previa es solo informativa y la validación final siempre es de la API."

> **5. Reportes.** "Agrega una página Reportes con una pestaña por cada reporte de la API, con filtros compartidos de período y producto. Muestra tablas con totales en el pie, tarjetas de métricas y barras simples en CSS (sin librerías de gráficos), y un botón de descarga que reutilice `?formato=csv` con los mismos filtros de la pantalla."

> **6. Verificación.** "Prueba cada pantalla contra la API en ejecución con el escenario de referencia, revisa que la consola y las peticiones de red no tengan errores inesperados y comprueba que se vea bien en 375 px de ancho."

## Cómo ejecutar el proyecto

Requisitos: SDK de .NET 10 y SQL Server LocalDB (instancia `MSSQLLocalDB`). Desde la carpeta `BaityMu` del repositorio:

```text
dotnet run --project src\BaityMu.Web --launch-profile http       → API y Swagger en http://localhost:5251/swagger
dotnet run --project src\BaityMu.Client --launch-profile http    → front en http://localhost:5282
dotnet test                                                       → 151 pruebas
```

La API crea la base de datos, aplica las migraciones y siembra la granja al arrancar. En Visual Studio, el perfil **"API + Front"** arranca los dos proyectos a la vez. La colección de peticiones está en `src/BaityMu.Web/BaityMu.Web.http`.

## Metodología de trabajo

- **GitHub Project** con 7 épicas y 21 historias de usuario como issues, movidas por columnas (Por hacer, En progreso, Hecho).
- **Una rama por historia** (`feature/hu-NN-...`) y un pull request que cierra su issue.
- **Commits por capa** con Conventional Commits (`feat(infra)`, `feat(services)`, `feat(web)`…), enlazados con `Refs #n`.
- **Cada historia** se cerró con sus pruebas en verde y una prueba en vivo contra SQL Server.
