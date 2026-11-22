# BaityMu — Convención de commits

Se usa **Conventional Commits** (https://www.conventionalcommits.org/es/v1.0.0/). Las palabras clave van en inglés, que es el estándar, y la descripción va en español.

## Formato

```text
tipo(alcance): descripción corta en imperativo

Cuerpo opcional: qué se hizo y por qué.

Refs #1
```

- **tipo**: qué clase de cambio es (tabla de abajo). Siempre en minúscula.
- **alcance** (opcional): la capa o parte afectada.
- **descripción**: en imperativo, en minúscula, sin punto final y con un máximo de unos 70 caracteres. Por ejemplo, *"agregar entidad Vaca"*, no *"Agregué la entidad Vaca."*.
- **pie**: el issue relacionado.

## Tipos

| Tipo | Cuándo usarlo | Ejemplo |
|---|---|---|
| `feat` | Funcionalidad nueva (avance de una historia de usuario) | `feat(web): agregar endpoint POST /api/vacas` |
| `fix` | Corrección de un error | `fix(services): no vender lotes vencidos en la venta FIFO` |
| `test` | Agregar o corregir pruebas unitarias | `test(domain): probar que la vaca nueva empieza en 0 L` |
| `refactor` | Cambiar código sin cambiar lo que hace | `refactor(services): extraer cálculo FIFO a un método` |
| `db` | Migraciones de EF Core y datos semilla (**seed**) | `db: agregar migración Inicial` · `db(seed): sembrar granja desde appsettings` |
| `build` | Proyectos, paquetes NuGet y SDK | `build(infra): instalar EF Core SqlServer 10.0.15` |
| `chore` | Estructura, configuración y tareas que no son funcionalidad | `chore: eliminar archivos de ejemplo de las plantillas` |
| `docs` | Documentación (README, historias, diagramas, Swagger) | `docs: agregar convención de commits` |
| `style` | Formato, espacios, nombres; sin cambio de lógica | `style(domain): ordenar usings` |

> `db` no es un tipo estándar de Conventional Commits, pero la especificación permite agregar tipos propios. Se acordó para que las migraciones y el seed se identifiquen fácilmente en el historial.

## Alcances de BaityMu

`domain` · `infra` · `services` · `web` · `tests` · `db` · `seed` · `docs` · o la historia: `hu-01`, `hu-13`, …

## Relacionar el commit con el issue (pie)

| Pie | Efecto en GitHub |
|---|---|
| `Refs #1` | Enlaza el commit al issue #1 sin cerrarlo. Se usa mientras se avanza. |
| `Closes #3` | **Cierra** el issue #3 cuando el commit llega a `main`, y la tarjeta pasa sola a **Hecho** en el tablero. Se usa en el último commit de una historia. |

Los números de issue de cada historia están en https://github.com/MonicaRuiz06/BaityMu/issues. Por ejemplo, la Épica 0 es #1 y la HU-01 es #3.

## Ciclo recomendado por historia (TDD, como pide el profesor)

Por ejemplo, para la **HU-01 Registrar una vaca** (issue #3):

```text
test(hu-01): pruebas de registro de vaca (producción inicial 0 L, nombre BaityCow)      Refs #3
feat(domain): agregar entidades Granja y Vaca                                           Refs #3
db: agregar migración AgregarGranjaYVacas                                                Refs #3
feat(infra): agregar GranjaRepository                                                    Refs #3
feat(services): agregar GranjaService.AgregarVacaAsync                                  Refs #3
feat(web): agregar POST /api/vacas                                                       Closes #3
```

## Ejemplo con cuerpo

```text
fix(services): rechazar venta cuando solo hay lotes vencidos

La venta tomaba existencias de lotes vencidos porque el filtro
comparaba con >= en lugar de >. Ahora se aplica RN-08.

Closes #19
```

## En Visual Studio

En **Ver → Cambios de Git**, el cuadro *"Escriba un mensaje"* acepta varias líneas:
- La primera línea es el título (`tipo(alcance): descripción`).
- Deja una línea en blanco.
- Después escribe el cuerpo y el pie (`Refs #n` o `Closes #n`).
