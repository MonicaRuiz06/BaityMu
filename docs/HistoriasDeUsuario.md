<p align="center"><img src="img/logo-baitymu.png" alt="Logo de BaityMu" width="260"></p>

# BaityMu — Historias de usuario

Taller final — Lenguajes de Programación, Universidad Santiago de Cali.
API Web en .NET con arquitectura en capas para la granja lechera de Baity.

## El problema: la granja de Baity

Baity tiene una granja con **n** vacas. La primera, la que empezó todo, se llama **BaityCow**. Cada día cada vaca da **l** litros de leche, así que la granja produce **n × l** litros diarios.

Con la leche de cada día, Baity hace **queso** o **yogurt**, y lo que elabora pasa a su **inventario**. Lo que se hace con la leche de un día no puede pasar de lo que dieron las vacas ese día. La leche que no se transforma se pierde: se reporta como *no aprovechada* y no se guarda.

Del inventario, Baity puede vender todo lo que quiera, sin límite diario, siempre que tenga existencias. Eso sí, cada venta despacha siempre el **producto más antiguo que no esté vencido**. Lo que se vence sin venderse se considera **dañado**.

Baity necesita una API que le permita:

- registrar sus vacas y la leche que dan cada día;
- transformar la leche en queso o yogurt;
- consultar el inventario;
- registrar las ventas;
- generar los reportes de ventas, leche producida por día, producto por día, producto total, producto dañado y producción de las vacas.

### Parámetros de la granja

Ningún valor puede quedar fijo en el código: todos son configurables.

| Símbolo | Significado | Valor inicial |
|---|---|---|
| **n** | Número de vacas activas de la granja | 10 |
| **l** | Litros de leche por vaca por día | 15 L |
| **I** | Inventario de lotes de queso y yogurt | Se alimenta al transformar la leche |
| Rendimiento | Litros de leche por unidad de producto | Queso: 10 L = 1 kg · Yogurt: 1 L = 1 L |
| Vida útil | Días entre la producción y el vencimiento | Queso: 30 días · Yogurt: 15 días |
| Precio | Precio de venta por unidad | Configurable por producto |

**Decisión confirmada: n es el número de vacas activas registradas.** Registrar una vaca aumenta n en 1 y retirarla lo disminuye en 1. n no se edita directamente desde la configuración de la granja.

### Reglas de negocio

| Código | Regla |
|---|---|
| RN-01 | La producción diaria es n × l y se registra una sola vez por fecha y por vaca. |
| RN-02 | Cada transformación elige un solo producto y consume leche de una fecha. La suma transformada no puede superar lo producido ese día. La leche sobrante es no aprovechada. |
| RN-03 | Cantidad de producto = litros usados ÷ rendimiento. |
| RN-04 | Cada transformación genera un lote con: producto, fecha de producción, fecha de vencimiento (producción + vida útil), cantidad inicial y cantidad disponible. |
| RN-05 | Las ventas no tienen tope diario; el único límite son las existencias no vencidas. |
| RN-06 | FIFO: la venta descuenta primero del lote más antiguo con existencias y no vencido, y sigue con el siguiente si no alcanza. |
| RN-07 | La venta es atómica: si no alcanza el producto no vencido, se rechaza completa (409) y el inventario no cambia. |
| RN-08 | Un lote está vencido cuando *fecha > fecha de vencimiento*. Al procesar vencimientos, lo disponible pasa a dañado. |
| RN-09 | Cada venta guarda precio unitario, cantidad, total y el detalle de los lotes de donde salió. |
| RN-10 | Las operaciones aceptan una fecha opcional; por defecto se usa la fecha actual de un `IClock`. |

## Requisitos

- **Requisitos funcionales**: son las historias de usuario HU-01 a HU-21 de este documento.
- **Requisitos no funcionales**: son los de la tabla siguiente, sacados de las secciones 4, 10 y 11 del taller y del ejemplo del profesor.

| Código | Requisito no funcional |
|---|---|
| RNF-01 | **Plataforma**: C# con .NET 10 y ASP.NET Core Web API, desarrollado en Visual Studio. |
| RNF-02 | **Arquitectura en capas**: Domain, Infrastructure, Services y Web, con dependencias `Web → Services → Infrastructure → Domain`. Domain no depende de nadie y los controllers no tienen lógica de negocio ni usan el `DbContext`. |
| RNF-03 | **Persistencia**: Entity Framework Core con migraciones sobre **SQL Server** (LocalDB en desarrollo). Cambiar de servidor solo requiere cambiar la cadena de conexión. |
| RNF-04 | **Integridad transaccional**: cada operación de escritura se confirma en una sola transacción (`IUnitOfWork`). La venta es atómica (RN-07). |
| RNF-05 | **API REST**: JSON en todas las respuestas, fechas ISO `yyyy-MM-dd`, enums como texto (`"Queso"`) y verbos y códigos HTTP correctos (200, 201, 204, 400, 404 y 409). |
| RNF-06 | **Manejo de errores centralizado**: un middleware global responde ProblemDetails. Los errores 500 no muestran detalles internos. |
| RNF-07 | **Documentación**: Swagger/OpenAPI, `README.md` con instrucciones y una colección de pruebas `.http`. |
| RNF-08 | **Calidad de código**: async/await, inyección de dependencias, patrón repositorio, DTOs (las entidades nunca se exponen), AutoMapper y nombres claros en español. |
| RNF-09 | **Configurabilidad**: n, l, rendimientos, vida útil y precios no quedan fijos en el código; se configuran en `appsettings.json` y en la base de datos. |
| RNF-10 | **Testabilidad**: el tiempo se obtiene de un `IClock` para simular días. Las pruebas unitarias se hacen con NUnit y cubren todas las pruebas mínimas del punto 9. |
| RNF-11 | **Trazabilidad**: nunca se borra información histórica. Las vacas se desactivan y cada venta guarda de qué lotes salió. |
| RNF-12 | **Control de versiones**: repositorio Git con `.gitignore` de Visual Studio. |
| RNF-13 | **CORS** habilitado para que un cliente web pueda consumir la API (igual que el ejemplo del profesor). |

## Roles

Todos los usuarios de BaityMu son Baity. Lo que cambia es el sombrero que lleva puesto:

| Rol | Qué hace en la granja |
|---|---|
| **BaityAdministrador** | Configura la granja: litros por vaca (l) y los parámetros de cada producto (rendimiento, vida útil y precio). |
| **BaityGanadero** | Registra y cuida las vacas, y anota la leche que da cada una cada día. |
| **BaityQuesero** | Transforma la leche del día en queso o en yogurt. |
| **BaityBodeguero** | Controla el inventario de lotes y saca lo que se venció. |
| **BaityVendedor** | Vende queso y yogurt, siempre del lote más antiguo que no esté vencido. |
| **BaityGerente** | Consulta los reportes para tomar decisiones. |

## Convenciones

- Los criterios usan el formato **Dado / Cuando / Entonces**.
- Las fechas son ISO (`yyyy-MM-dd`).
- Los errores se responden en formato ProblemDetails: **400** (datos inválidos), **404** (no existe) y **409** (conflicto con el estado de la granja).
- Las **tareas de desarrollo** se organizan por capa, como en el ejemplo del profesor: **Dominio**, **Repos** (Infrastructure), **Servicios**, **Web** y **Unit test**.
- Arquitectura: `Web → Services → Infrastructure → Domain`. Las interfaces de los repositorios, `IUnitOfWork` e `IClock` viven en Domain, y los controllers nunca usan el `DbContext`.

---

## Épica 0 — Base del proyecto (tareas transversales)

Este bloque no es una historia: es lo que hay que montar antes de empezar con la HU-01.

- **Solución**: crear `BaityMu.sln` en .NET 10 con los proyectos `BaityMu.Domain`, `BaityMu.Infrastructure`, `BaityMu.Services`, `BaityMu.Web` y `BaityMu.Tests`, con sus referencias entre capas.
- **Dominio**: crear el enum `TipoProducto` (Queso, Yogurt), la jerarquía de excepciones de dominio (regla de negocio → 400, no encontrado → 404, conflicto → 409) y las interfaces `IRepository<T>`, `IUnitOfWork` e `IClock`.
- **Repos**: instalar EF Core con SQL Server, crear `BaityMuDbContext`, implementar `UnitOfWork` (un solo `SaveChanges`, una sola transacción) y `SystemClock`, y crear `DependencyInjection.AddRepositories(...)`.
- **Servicios**: crear `DependencyInjection.AddServices()`.
- **Web**: configurar `Program.cs` con inyección de dependencias, Swagger, AutoMapper y CORS. Crear el middleware global de errores que traduce las excepciones a ProblemDetails, aplicar las migraciones al arrancar, y crear `appsettings.json` con la cadena de conexión y los valores iniciales de la granja.
- **Unit test**: crear el proyecto de pruebas con **NUnit** (igual que el ejemplo del profesor), un `FakeClock` para simular días y el proveedor EF Core InMemory para las pruebas de servicios (no requieren SQL Server).
- **Documentación**: crear el `README.md` con las instrucciones de ejecución y el archivo `BaityMu.http` con el escenario de referencia.

---

## Épica 1 — Granja y vacas

### HU-01 Registrar una vaca

**Como** BaityGanadero
**quiero** registrar una vaca en la aplicación
**para** poder controlar su producción de leche.

**Criterios de aceptación**

1. **Dado** que envío una raza válida (y un nombre opcional), **cuando** registro la vaca, **entonces** se crea con un Id, queda activa y su producción de leche es **0 L**.
2. **Dado** que no envío nombre, **cuando** registro la vaca, **entonces** el sistema le asigna uno automáticamente (la primera vaca se llama *BaityCow*).
3. **Dado** que la raza viene vacía, **cuando** intento registrar la vaca, **entonces** recibo **400** y la vaca no se crea.
4. **Dado** que registré una vaca, **cuando** consulto la granja, **entonces** n aumentó en 1.
5. **Dado** que la vaca ya está registrada, **cuando** se registre su producción diaria, **entonces** queda la trazabilidad de los litros de cada día.

**Tareas de desarrollo**

- **Unit test**: escribir primero las pruebas: la vaca nueva tiene 0 L; sin nombre se llama BaityCow; con raza vacía se rechaza; n aumenta.
- **Dominio**: crear la entidad `Vaca` (Id, Nombre, Raza, Activa, FechaIngreso) con sus validaciones, y crear la entidad `Granja` con `AgregarVaca(...)` y n calculado como el número de vacas activas.
- **Repos**: configurar `Vaca` y `Granja` en el `DbContext`, crear `IGranjaRepository` y `GranjaRepository`, crear la migración y actualizar la base de datos.
- **Servicios**: crear `GranjaService.AgregarVacaAsync(...)` y los DTOs `CrearVacaRequest` y `VacaResponse`.
- **Web**: crear `VacasController` con `POST /api/vacas` (201) y el mapeo de AutoMapper `Vaca → VacaResponse`.

### HU-02 Ver el listado de vacas

**Como** BaityGanadero
**quiero** ver el listado de vacas de la granja
**para** conocer las vacas que tengo registradas.

**Criterios de aceptación**

1. **Dado** que hay vacas registradas, **cuando** consulto el listado, **entonces** veo de cada una el **Id**, el **nombre**, la **raza** y su **producción de leche en litros**.
2. **Dado** que una vaca nunca ha sido ordeñada, **cuando** consulto el listado, **entonces** aparece con **0 L**.
3. **Dado** que no hay vacas registradas, **cuando** consulto el listado, **entonces** recibo **200** con una lista vacía.
4. **Dado** que una vaca fue retirada, **cuando** consulto el listado sin filtros, **entonces** no aparece; **y cuando** pido incluir las retiradas, **entonces** aparece marcada como inactiva.

**Tareas de desarrollo**

- **Unit test**: el listado trae los litros de cada vaca; una vaca sin ordeñar sale con 0 L; las retiradas se filtran.
- **Dominio**: nada nuevo; se reutiliza `Vaca`.
- **Repos**: agregar en `IProduccionRepository` una consulta que sume los litros por vaca.
- **Servicios**: crear `GranjaService.ListarVacasAsync(incluirRetiradas)`, que arma un `VacaResponse` con los litros totales.
- **Web**: agregar `GET /api/vacas?incluirRetiradas=` (200).

### HU-03 Consultar una vaca

**Como** BaityGanadero
**quiero** consultar una vaca por su Id
**para** ver sus datos y su historial de producción.

**Criterios de aceptación**

1. **Dado** que la vaca existe, **cuando** la consulto, **entonces** veo sus datos, su producción total y los litros de cada día registrado.
2. **Dado** que el Id no existe, **cuando** la consulto, **entonces** recibo **404**.

**Tareas de desarrollo**

- **Unit test**: consultar una vaca existente devuelve su historial; un Id inexistente lanza "no encontrado".
- **Dominio**: nada nuevo.
- **Repos**: agregar `IProduccionRepository.ListarPorVacaAsync(vacaId)`.
- **Servicios**: crear `GranjaService.ObtenerVacaAsync(id)` y el DTO `VacaDetalleResponse` con su historial.
- **Web**: agregar `GET /api/vacas/{id}` (200 o 404).

### HU-04 Retirar una vaca

**Como** BaityGanadero
**quiero** retirar una vaca que ya no produce
**para** que no cuente dentro de las n vacas de la granja.

**Criterios de aceptación**

1. **Dado** que la vaca está activa, **cuando** la retiro, **entonces** queda inactiva, n disminuye en 1 y **su historial de producción se conserva**.
2. **Dado** que es la última vaca activa, **cuando** intento retirarla, **entonces** recibo **400** (la granja necesita al menos una vaca).
3. **Dado** que el Id no existe, **cuando** intento retirarla, **entonces** recibo **404**.

**Tareas de desarrollo**

- **Unit test**: retirar una vaca baja n; no se puede retirar la última; el historial se conserva.
- **Dominio**: agregar `Granja.RetirarVaca(id)` con la regla de la última vaca. Las vacas se desactivan, nunca se borran.
- **Repos**: nada nuevo; se guarda con `IUnitOfWork`.
- **Servicios**: crear `GranjaService.RetirarVacaAsync(id)`.
- **Web**: agregar `DELETE /api/vacas/{id}` (204, 400 o 404).

### HU-05 Consultar la configuración de la granja

**Como** BaityAdministrador
**quiero** consultar la configuración de la granja
**para** saber con qué parámetros está trabajando.

**Criterios de aceptación**

1. **Dado** que la granja está configurada, **cuando** consulto `GET /api/granja`, **entonces** recibo **200** con n, l, la producción diaria esperada (n × l) y, para cada producto, su unidad, rendimiento, vida útil y precio.
2. **Dado** el escenario de referencia, **cuando** consulto la granja, **entonces** la producción diaria esperada es **150 L**.

**Tareas de desarrollo**

- **Unit test**: con 10 vacas y 15 L, la producción esperada es 150 L.
- **Dominio**: crear la entidad `ParametroProducto` (Tipo, Unidad, LitrosPorUnidad, VidaUtilDias, PrecioUnitario) y agregar a `Granja` los campos `LitrosPorVaca` y `ProduccionDiariaEsperada`.
- **Repos**: configurar `ParametroProducto` en el `DbContext`, crear la migración y sembrar la granja inicial a partir de `appsettings.json`.
- **Servicios**: crear `GranjaService.ObtenerAsync()` y los DTOs `GranjaResponse` y `ParametroProductoResponse`.
- **Web**: crear `GranjaController` con `GET /api/granja` (200).

### HU-06 Actualizar la configuración de la granja

**Como** BaityAdministrador
**quiero** actualizar l y los parámetros de rendimiento, vida útil y precio de cada producto
**para** adaptar la granja sin modificar el código.

**Criterios de aceptación**

1. **Dado** que envío valores válidos, **cuando** actualizo con `PUT /api/granja`, **entonces** recibo **200** con la configuración nueva.
2. **Dado** que l, el rendimiento o la vida útil son ≤ 0, o que el precio es negativo, **cuando** actualizo, **entonces** recibo **400** y nada cambia.
3. **Dado** que cambié los parámetros, **cuando** se crean lotes o ventas nuevas, **entonces** usan los valores nuevos; **y** los lotes y ventas anteriores conservan los valores con los que se crearon.
4. **Dado** que la aplicación arranca por primera vez, **cuando** se crea la granja, **entonces** los valores iniciales se toman de `appsettings.json` y no del código.

**Tareas de desarrollo**

- **Unit test**: los valores inválidos se rechazan sin cambiar nada; un lote creado antes del cambio conserva su vencimiento y una venta anterior conserva su precio.
- **Dominio**: agregar `Granja.ActualizarLitrosPorVaca(...)`, `Granja.ConfigurarProducto(...)` y las validaciones de `ParametroProducto.Actualizar(...)`.
- **Repos**: nada nuevo.
- **Servicios**: crear `GranjaService.ActualizarAsync(...)` y el DTO `ActualizarGranjaRequest` con sus validaciones.
- **Web**: agregar `PUT /api/granja` (200 o 400).

---

## Épica 2 — Producción de leche

### HU-07 Registrar la producción diaria

**Como** BaityGanadero
**quiero** registrar la leche que dieron las vacas en un día
**para** saber cuánta leche hay disponible para transformar.

**Criterios de aceptación**

1. **Dado** que hay n vacas activas, **cuando** registro la producción de una fecha sin detallar litros, **entonces** a cada vaca se le registran l litros y el total del día es **n × l** (RN-01).
2. **Dado** el escenario de referencia, **cuando** registro la producción del día 1, **entonces** el total es **150 L**.
3. **Dado** que envío litros específicos para algunas vacas, **cuando** registro la producción, **entonces** se guardan esos litros solo para esas vacas.
4. **Dado** que una vaca ya tiene producción registrada en esa fecha, **cuando** intento registrarla otra vez, **entonces** recibo **409** y no se guarda nada (RN-01).
5. **Dado** que una vaca no existe o está retirada, **cuando** intento registrarle producción, **entonces** recibo **404**.
6. **Dado** que los litros son negativos, **cuando** registro la producción, **entonces** recibo **400**.
7. **Dado** que no envío fecha, **cuando** registro la producción, **entonces** se usa la fecha actual del `IClock` (RN-10).

**Tareas de desarrollo**

- **Unit test** *(mínima del taller)*: la producción diaria es n × l y no se puede registrar dos veces para la misma fecha.
- **Dominio**: crear la entidad `ProduccionLeche` (Vaca, Fecha, Litros) con sus validaciones y la excepción `ProduccionDuplicadaException` (409).
- **Repos**: configurar `ProduccionLeche` con un índice único (VacaId, Fecha), crear `IProduccionRepository` y `ProduccionRepository`, y crear la migración.
- **Servicios**: crear `ProduccionService.RegistrarAsync(...)` y los DTOs `RegistrarProduccionRequest` y `ProduccionDiariaResponse`.
- **Web**: crear `ProduccionController` con `POST /api/produccion` (201, 400, 404 o 409).

### HU-08 Consultar la producción de un día

**Como** BaityGanadero
**quiero** consultar la producción de una fecha
**para** ver cuánta leche dio cada vaca y cuánta queda sin transformar.

**Criterios de aceptación**

1. **Dado** que hay producción en esa fecha, **cuando** la consulto, **entonces** veo los litros de cada vaca, el total producido, los litros ya transformados y los litros que quedan disponibles.
2. **Dado** que no hay producción en esa fecha, **cuando** la consulto, **entonces** recibo **404**.

**Tareas de desarrollo**

- **Unit test**: después de transformar 100 L de 150 L, quedan disponibles 50 L.
- **Dominio**: nada nuevo.
- **Repos**: agregar `IProduccionRepository.ListarPorFechaAsync(fecha)` e `ILoteRepository.LitrosTransformadosAsync(fecha)`.
- **Servicios**: crear `ProduccionService.ObtenerAsync(fecha)`.
- **Web**: agregar `GET /api/produccion/{fecha}` (200 o 404).

---

## Épica 3 — Transformación

### HU-09 Transformar la leche del día en queso o yogurt

**Como** BaityQuesero
**quiero** transformar la leche de un día en queso o en yogurt
**para** obtener producto que pueda venderse.

**Criterios de aceptación**

1. **Dado** que hay leche disponible en esa fecha, **cuando** transformo una cantidad de litros en **un solo** producto, **entonces** se crea un **lote** con: producto, fecha de producción, fecha de vencimiento (producción + vida útil), litros usados, cantidad inicial y cantidad disponible (RN-02, RN-04).
2. **Dado** que el rendimiento del queso es 10 L/kg, **cuando** transformo **100 L** en queso, **entonces** el lote tiene **10 kg** (RN-03).
3. **Dado** que el rendimiento del yogurt es 1 L/L y su vida útil es de 15 días, **cuando** transformo **50 L** en yogurt el 2026-12-01, **entonces** el lote tiene **50 L** y vence el **2026-12-16**.
4. **Dado** que ese día se produjeron 150 L y ya se transformaron 100 L, **cuando** intento transformar **60 L**, **entonces** recibo **409** y no se crea el lote (RN-02).
5. **Dado** que no hay producción registrada en esa fecha, **cuando** intento transformar, **entonces** recibo **409**.
6. **Dado** que los litros son ≤ 0 o el producto no existe, **cuando** intento transformar, **entonces** recibo **400**.
7. **Dado** que se hacen varias transformaciones el mismo día, **cuando** se registran, **entonces** cada una genera su propio lote y la suma de litros transformados nunca supera lo producido.
8. **Dado** que no se transformó toda la leche del día, **cuando** se consulta ese día, **entonces** la leche sobrante aparece como **no aprovechada** y no se almacena.

**Tareas de desarrollo**

- **Unit test** *(mínima del taller)*: no se pueden transformar más litros de los disponibles ese día. Además: 100 L dan 10 kg de queso; el yogurt vence a los 15 días.
- **Dominio**: crear la entidad `LoteProducto` con `Crear(parametro, fecha, litros)`, agregar `ParametroProducto.CalcularCantidad(...)` y `CalcularVencimiento(...)`, y crear la excepción `LecheInsuficienteException` (409).
- **Repos**: configurar `LoteProducto` en el `DbContext`, crear `ILoteRepository` y `LoteRepository`, y crear la migración.
- **Servicios**: crear `ProduccionService.TransformarAsync(...)` y los DTOs `TransformarLecheRequest` y `LoteResponse`.
- **Web**: crear `TransformacionesController` con `POST /api/transformaciones` (201, 400 o 409) y el mapeo `LoteProducto → LoteResponse`.

---

## Épica 4 — Inventario

### HU-10 Consultar el inventario

**Como** BaityBodeguero
**quiero** ver los lotes del inventario
**para** saber qué producto tengo, cuánto queda y cuándo vence.

**Criterios de aceptación**

1. **Dado** que hay lotes, **cuando** consulto el inventario, **entonces** veo de cada lote: Id, producto, fecha de producción, fecha de vencimiento, cantidad inicial, disponible, dañada y su **estado** (Disponible, Agotado, Vencido o Dañado).
2. **Dado** que filtro por producto (queso o yogurt), **cuando** consulto, **entonces** solo veo los lotes de ese producto.
3. **Dado** que consulto a una fecha determinada, **cuando** un lote tiene existencias y esa fecha es posterior a su vencimiento, **entonces** aparece como **Vencido**.
4. **Dado** que consulto un lote por Id, **cuando** no existe, **entonces** recibo **404**.
5. **Dado** que pido el resumen del inventario, **cuando** lo consulto, **entonces** veo por producto la cantidad vendible, la vencida sin procesar y la dañada.

**Tareas de desarrollo**

- **Unit test**: el estado del lote cambia según la fecha (incluido el borde: el día del vencimiento todavía no está vencido).
- **Dominio**: crear el enum `EstadoLote` y agregar `LoteProducto.EstaVencido(fecha)` y `LoteProducto.ObtenerEstado(fecha)`.
- **Repos**: agregar `ILoteRepository.ListarAsync(tipo, desde, hasta)` y `ObtenerPorIdAsync(id)`.
- **Servicios**: crear `InventarioService.ListarLotesAsync(...)`, `ObtenerLoteAsync(...)` y `ObtenerResumenAsync(...)`.
- **Web**: crear `InventarioController` con `GET /api/inventario`, `GET /api/inventario/lotes/{id}` y `GET /api/inventario/resumen`.

### HU-11 Procesar vencimientos

**Como** BaityBodeguero
**quiero** procesar los lotes vencidos
**para** sacarlos de la venta y registrarlos como producto dañado.

**Criterios de aceptación**

1. **Dado** que un lote tiene existencias y la fecha de proceso es posterior a su vencimiento, **cuando** proceso vencimientos, **entonces** su cantidad disponible pasa a **cantidad dañada** y queda en 0 (RN-08).
2. **Dado** el escenario de referencia, **cuando** proceso vencimientos el día 17 (2026-12-17), **entonces** los **30 L** de yogurt del lote Y1 pasan a dañados.
3. **Dado** que un lote vence exactamente en la fecha de proceso, **cuando** proceso vencimientos, **entonces** **no** se marca como dañado (está vencido solo cuando *fecha > vencimiento*).
4. **Dado** que un lote ya está agotado, **cuando** proceso vencimientos, **entonces** no se modifica.
5. **Dado** que terminó el proceso, **cuando** veo la respuesta, **entonces** muestra los lotes afectados y el total dañado por producto.
6. **Dado** que ya procesé una fecha, **cuando** la vuelvo a procesar, **entonces** no se duplica el producto dañado.

**Tareas de desarrollo**

- **Unit test** *(mínima del taller)*: al procesar vencimientos, la cantidad disponible pasa a dañada y el reporte lo refleja. Además: procesar dos veces no duplica.
- **Dominio**: agregar `LoteProducto.MarcarComoDanado()`.
- **Repos**: agregar `ILoteRepository.ObtenerVencidosConExistenciasAsync(fecha)`.
- **Servicios**: crear `InventarioService.ProcesarVencimientosAsync(fecha)` y el DTO `ProcesarVencimientosResponse`.
- **Web**: agregar `POST /api/inventario/procesar-vencimientos` (200).

### HU-12 Ver lotes próximos a vencer *(opcional)*

**Como** BaityBodeguero
**quiero** ver los lotes que vencen pronto
**para** venderlos antes de que se dañen.

**Criterios de aceptación**

1. **Dado** que indico un número de días (por defecto 3), **cuando** consulto, **entonces** veo los lotes con existencias que vencen dentro de ese plazo.
2. **Dado** que un lote ya está vencido o agotado, **cuando** consulto, **entonces** no aparece.

**Tareas de desarrollo**

- **Unit test**: con un plazo de 3 días aparece el lote que vence en 2 días y no aparece el que ya venció.
- **Dominio**: nada nuevo.
- **Repos**: reutilizar `ILoteRepository.ListarAsync(...)`.
- **Servicios**: crear `InventarioService.ListarProximosAVencerAsync(dias, fecha)`.
- **Web**: agregar `GET /api/inventario/proximos-a-vencer?dias=3`.

---

## Épica 5 — Ventas

### HU-13 Registrar una venta

**Como** BaityVendedor
**quiero** vender una cantidad de queso o de yogurt
**para** despachar siempre el producto más antiguo que no esté vencido.

**Criterios de aceptación**

1. **Dado** que hay existencias suficientes, **cuando** registro una venta, **entonces** recibo **201** con: Id, fecha, producto, cantidad, precio unitario, total y el **detalle de los lotes** de donde salió (RN-09).
2. **Dado** que hay varios lotes, **cuando** vendo, **entonces** se descuenta primero del lote con la **fecha de producción más antigua** (FIFO, RN-06).
3. **Dado** que el lote más antiguo no alcanza, **cuando** vendo, **entonces** se toma el resto del siguiente lote y la venta tiene **varios detalles**. Ejemplo: vender **12 kg** de queso el día 3 saca **10 kg de Q1** (que queda agotado) y **2 kg de Q2** (quedan 13 kg).
4. **Dado** que el lote más antiguo está **vencido**, **cuando** vendo, **entonces** ese lote se salta y nunca se vende (RN-06, RN-08).
5. **Dado** que las existencias no vencidas no alcanzan, **cuando** intento vender, **entonces** recibo **409**, la venta se rechaza completa y **el inventario no cambia** (RN-07). Ejemplo: vender 5 L de yogurt el día 17.
6. **Dado** que no hay límite diario, **cuando** registro muchas ventas el mismo día, **entonces** todas se aceptan mientras haya existencias (RN-05).
7. **Dado** que la cantidad es ≤ 0 o el producto no existe, **cuando** intento vender, **entonces** recibo **400**.
8. **Dado** que un lote se produjo después de la fecha de la venta, **cuando** vendo, **entonces** ese lote no se usa.
9. **Dado** que la venta se acepta, **cuando** se guarda, **entonces** la venta y los cambios en los lotes se confirman **en una sola transacción**.

**Tareas de desarrollo**

- **Unit test** *(mínimas del taller)*:
  - Una venta toma del lote más antiguo antes que de uno más nuevo.
  - Una venta que excede un lote consume el siguiente y genera varios detalles.
  - Un lote vencido nunca se vende, aunque sea el más antiguo.
  - Una venta con existencias insuficientes no modifica el inventario.
- **Dominio**: crear las entidades `Venta` (con `AgregarDetalle(lote, cantidad)` y el cálculo del total) y `VentaDetalle` (lote, fecha de producción, cantidad), agregar `LoteProducto.Descontar(cantidad)` y crear la excepción `InventarioInsuficienteException` (409).
- **Repos**: configurar `Venta` y `VentaDetalle` en el `DbContext`, agregar `ILoteRepository.ObtenerVendiblesAsync(tipo, fecha)` (no vencidos, con existencias y ordenados por fecha de producción ascendente), crear `IVentaRepository` y `VentaRepository`, y crear la migración.
- **Servicios**: crear `VentaService.RegistrarVentaAsync(...)` con la lógica FIFO de la sección 7 del taller y un solo `IUnitOfWork.SaveChangesAsync()`, y los DTOs `VentaRequest`, `VentaResponse` y `VentaDetalleResponse`.
- **Web**: crear `VentasController` con `POST /api/ventas` (201, 400 o 409) y el mapeo `Venta → VentaResponse`.

### HU-14 Consultar ventas

**Como** BaityVendedor
**quiero** consultar una venta o el listado de ventas
**para** ver su trazabilidad.

**Criterios de aceptación**

1. **Dado** que la venta existe, **cuando** la consulto por Id, **entonces** veo todos sus datos y los lotes de donde salió el producto.
2. **Dado** que el Id no existe, **cuando** la consulto, **entonces** recibo **404**.
3. **Dado** que filtro por rango de fechas o por producto, **cuando** listo las ventas, **entonces** solo veo las que cumplen el filtro.

**Tareas de desarrollo**

- **Unit test**: consultar una venta trae sus detalles; un Id inexistente lanza "no encontrado".
- **Dominio**: nada nuevo.
- **Repos**: agregar `IVentaRepository.ObtenerPorIdAsync(id)` (con sus detalles) y `ListarAsync(desde, hasta, tipo)`.
- **Servicios**: crear `VentaService.ObtenerAsync(id)` y `ListarAsync(...)`.
- **Web**: agregar `GET /api/ventas/{id}` (200 o 404) y `GET /api/ventas?desde=&hasta=&tipo=`.

---

## Épica 6 — Reportes

Reglas comunes a todos los reportes con rango:

- **Dado** que `desde` es posterior a `hasta`, **cuando** pido el reporte, **entonces** recibo **400**.
- Si no hay datos, se responde **200** con listas vacías y totales en 0.

Tareas comunes: crear `ReporteService` en Servicios y `ReportesController` en Web (`/api/reportes/...`), y validar el rango de fechas en un solo lugar.

### HU-15 Reporte de ventas

**Como** BaityGerente
**quiero** ver las ventas de un período
**para** conocer cuánto se vendió y cuánto dinero entró.

**Criterios de aceptación**

1. **Dado** un rango de fechas (y opcionalmente un producto), **cuando** pido el reporte, **entonces** veo por cada venta: fecha, producto, cantidad, precio unitario y total.
2. **Dado** que hay ventas, **cuando** veo el reporte, **entonces** incluye el total por producto y el **total general**.

**Tareas de desarrollo**

- **Unit test**: con las ventas del escenario, el total general es correcto y el filtro por producto funciona.
- **Repos**: reutilizar `IVentaRepository.ListarAsync(...)`.
- **Servicios**: crear `ReporteService.VentasAsync(desde, hasta, tipo)` y el DTO `ReporteVentasResponse`.
- **Web**: agregar `GET /api/reportes/ventas?desde=&hasta=&tipo=`.

### HU-16 Reporte de leche por día

**Como** BaityGerente
**quiero** ver la leche producida cada día
**para** saber cuánta se aprovechó y cuánta se perdió.

**Criterios de aceptación**

1. **Dado** un rango de fechas, **cuando** pido el reporte, **entonces** veo por día: litros producidos, litros transformados y litros **no aprovechados**.
2. **Dado** el escenario de referencia, **cuando** pido el reporte, **entonces** muestra **150 L** el día 1 y **150 L** el día 2.

**Tareas de desarrollo**

- **Unit test**: el día 1 y el día 2 muestran 150 L producidos y 0 L no aprovechados.
- **Repos**: reutilizar `IProduccionRepository.ListarAsync(desde, hasta)` e `ILoteRepository.ListarAsync(...)`.
- **Servicios**: crear `ReporteService.LecheDiariaAsync(desde, hasta)` y el DTO `ReporteLecheDiariaResponse`.
- **Web**: agregar `GET /api/reportes/leche-diaria?desde=&hasta=`.

### HU-17 Reporte de producto por día

**Como** BaityGerente
**quiero** ver cuánto producto se elaboró cada día
**para** conocer la producción de queso y yogurt.

**Criterios de aceptación**

1. **Dado** un rango de fechas, **cuando** pido el reporte, **entonces** veo por día y por producto la cantidad elaborada, con su unidad (kg o L) y los litros usados.
2. **Dado** el escenario de referencia, **cuando** pido el reporte, **entonces** el día 1 muestra 10 kg de queso y 50 L de yogurt, y el día 2 muestra 15 kg de queso.

**Tareas de desarrollo**

- **Unit test**: el resultado coincide con el escenario de referencia.
- **Repos**: reutilizar `ILoteRepository.ListarAsync(...)`.
- **Servicios**: crear `ReporteService.ProductoDiarioAsync(desde, hasta)` y el DTO `ReporteProductoDiarioResponse`.
- **Web**: agregar `GET /api/reportes/producto-diario?desde=&hasta=`.

### HU-18 Reporte de producto total

**Como** BaityGerente
**quiero** ver el balance de cada producto
**para** saber cuánto se produjo, vendió, queda y se dañó.

**Criterios de aceptación**

1. **Dado** que hay lotes, **cuando** pido el reporte, **entonces** veo por producto: total **producido**, **vendido**, **disponible** y **dañado**.
2. **Dado** el escenario de referencia completo (hasta el día 17), **cuando** pido el reporte, **entonces** el resultado es:

| Producto | Producido | Vendido | Disponible | Dañado |
|---|---|---|---|---|
| Queso (kg) | 25 | 12 | 13 | 0 |
| Yogurt (L) | 50 | 20 | 0 | 30 |

**Tareas de desarrollo**

- **Unit test**: la tabla anterior se cumple exactamente.
- **Repos**: reutilizar `ILoteRepository.ListarAsync(...)` e `IVentaRepository.ListarAsync(...)`.
- **Servicios**: crear `ReporteService.ProductoTotalAsync()` y el DTO `ProductoTotalItem`.
- **Web**: agregar `GET /api/reportes/producto-total`.

### HU-19 Reporte de producto dañado

**Como** BaityGerente
**quiero** ver el producto que se dañó
**para** saber cuánto se perdió por vencimiento.

**Criterios de aceptación**

1. **Dado** un rango de fechas de vencimiento, **cuando** pido el reporte, **entonces** veo cada lote dañado con: producto, fecha de producción, **fecha de vencimiento** y cantidad dañada.
2. **Dado** que hay lotes dañados, **cuando** veo el reporte, **entonces** incluye el total dañado por producto.
3. **Dado** el escenario de referencia, **cuando** pido el reporte después de procesar vencimientos el día 17, **entonces** aparece el lote Y1 con **30 L** de yogurt.

**Tareas de desarrollo**

- **Unit test**: después de procesar vencimientos, el lote Y1 aparece con 30 L.
- **Repos**: agregar `ILoteRepository.ListarDanadosAsync(tipo, desde, hasta)`.
- **Servicios**: crear `ReporteService.ProductoDanadoAsync(desde, hasta, tipo)` y el DTO `ReporteProductoDanadoResponse`.
- **Web**: agregar `GET /api/reportes/producto-danado?desde=&hasta=`.

### HU-20 Reporte de vacas

**Como** BaityGerente
**quiero** ver la producción de cada vaca
**para** saber cuáles rinden más y cuáles están por debajo.

**Criterios de aceptación**

1. **Dado** un rango de fechas, **cuando** pido el reporte, **entonces** veo la **producción promedio** diaria del hato.
2. **Dado** que hay producción, **cuando** veo el reporte, **entonces** muestra la vaca que **más produce** y la que **menos produce**.
3. **Dado** que hay producción, **cuando** veo el reporte, **entonces** lista las vacas **por debajo del promedio** y cuánto les falta para alcanzarlo.
4. **Dado** que todas las vacas producen lo mismo, **cuando** veo el reporte, **entonces** ninguna aparece por debajo del promedio.
5. **Dado** que no hay producción en el rango, **cuando** pido el reporte, **entonces** recibo **200** sin vacas evaluadas.

**Tareas de desarrollo**

- **Unit test**: con litros distintos por vaca (por ejemplo, BaityCow 20 L y otra vaca 10 L), se identifican correctamente la mayor, la menor y las que están por debajo del promedio; con litros iguales, ninguna queda por debajo.
- **Repos**: reutilizar `IProduccionRepository.ListarAsync(desde, hasta)` (incluyendo los datos de la vaca).
- **Servicios**: crear `ReporteService.VacasAsync(desde, hasta)` y el DTO `ReporteVacasResponse`.
- **Web**: agregar `GET /api/reportes/vacas?desde=&hasta=`.

### HU-21 Exportar reportes a CSV *(opcional)*

**Como** BaityGerente
**quiero** descargar los reportes en CSV
**para** abrirlos en Excel.

**Criterios de aceptación**

1. **Dado** cualquier reporte, **cuando** lo pido en formato CSV, **entonces** recibo un archivo `.csv` con los mismos datos que la versión JSON.

**Tareas de desarrollo**

- **Unit test**: el CSV del reporte de producto total tiene el encabezado y las 2 filas esperadas.
- **Servicios**: crear un `CsvExporter` que convierta una lista de DTOs en CSV.
- **Web**: aceptar `?formato=csv` en los endpoints de reportes y devolver un `File(...)` con `text/csv`.

---

## Escenario de referencia (prueba de aceptación integral)

| Día | Rol | Acción | Resultado esperado |
|---|---|---|---|
| 1 (2026-12-01) | BaityGanadero | Registra la producción | 150 L |
| 1 | BaityQuesero | Transforma 100 L en queso y 50 L en yogurt | Q1 = 10 kg; Y1 = 50 L, vence el 2026-12-16 |
| 2 (2026-12-02) | BaityGanadero | Registra la producción | 150 L |
| 2 | BaityQuesero | Transforma 150 L en queso | Q2 = 15 kg |
| 3 (2026-12-03) | BaityVendedor | Vende 12 kg de queso | 10 kg de Q1 y 2 kg de Q2 |
| 3 | BaityVendedor | Vende 20 L de yogurt | Salen de Y1; quedan 30 L |
| 17 (2026-12-17) | BaityBodeguero | Procesa vencimientos | Y1: 30 L pasan a dañado |
| 17 | BaityVendedor | Vende 5 L de yogurt | **409**: no hay existencias no vencidas |
| 17 | BaityGerente | Pide el reporte de producto total | Queso 25/12/13/0 · Yogurt 50/20/0/30 |

**Tareas de desarrollo**: una prueba de integración que ejecuta este escenario completo con un `FakeClock`, y las mismas peticiones en el archivo `BaityMu.http` para correrlas a mano desde Visual Studio.

## Cobertura de las pruebas mínimas del taller (punto 9)

| Prueba mínima | Historia |
|---|---|
| La producción diaria es n × l y no se registra dos veces la misma fecha | HU-07 |
| No se transforman más litros de los disponibles ese día | HU-09 |
| La venta toma del lote más antiguo antes que de uno más nuevo | HU-13 |
| La venta que excede un lote consume el siguiente y genera varios detalles | HU-13 |
| Un lote vencido nunca se vende, aunque sea el más antiguo | HU-13 |
| Una venta con existencias insuficientes no modifica el inventario | HU-13 |
| Al procesar vencimientos lo disponible pasa a dañado y el reporte lo refleja | HU-11, HU-19 |
