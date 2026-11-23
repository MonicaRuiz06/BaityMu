using AutoMapper;
using BaityMu.Domain.Entities;
using BaityMu.Services.Dtos;
using BaityMu.Services.Modelos;

namespace BaityMu.Web.Mapping
{
    /// <summary>
    /// Perfil de AutoMapper de BaityMu: define cómo se convierten las entidades del
    /// dominio en los DTOs que devuelve la API.
    /// </summary>
    /// <remarks>
    /// ¿Qué es AutoMapper?
    /// Una librería que copia los datos de un objeto a otro según reglas declaradas una
    /// sola vez. Evita escribir a mano "dto.Id = vaca.Id; dto.Nombre = vaca.Nombre; ...".
    ///
    /// ¿Por qué hace falta?
    /// Las entidades del dominio nunca se exponen directamente (RNF-08): la API responde
    /// con DTOs. AutoMapper hace esa conversión.
    ///
    /// Cada historia agrega aquí sus mapeos.
    ///
    /// Se registra en Program.cs con AddAutoMapper.
    /// </remarks>
    public class BaityMuProfile : Profile
    {
        /// <summary>
        /// Declara los mapeos. AutoMapper ejecuta este constructor una sola vez, al arrancar.
        /// </summary>
        public BaityMuProfile()
        {
            // HU-05: la configuración de la granja. NumeroVacas y ProduccionDiariaEsperada
            // se copian de las propiedades calculadas de Granja porque se llaman igual.
            CreateMap<Granja, GranjaResponse>();
            CreateMap<ParametroProducto, ParametroProductoResponse>();

            // HU-07: la producción de un día.
            CreateMap<ProduccionDiaria, ProduccionDiariaResponse>();
            CreateMap<ProduccionDeVaca, ProduccionVacaDiaResponse>();

            // HU-09: los lotes del inventario. Todas las propiedades se llaman igual.
            CreateMap<LoteProducto, LoteResponse>();

            // HU-01: las propiedades con el mismo nombre se copian solas (Id, Nombre,
            // Raza, Activa, FechaIngreso). LitrosProducidos no existe en la entidad:
            // queda en 0 hasta que la HU-07 registre la producción.
            CreateMap<Vaca, VacaResponse>()
                .ForMember(d => d.LitrosProducidos, o => o.Ignore());

            // HU-02: el listado trae la vaca y sus litros. IncludeMembers toma Id, Nombre,
            // Raza... de la vaca (con el mapeo de arriba), y LitrosProducidos se copia
            // solo porque se llama igual en los dos lados.
            CreateMap<VacaConLitros, VacaResponse>()
                .IncludeMembers(x => x.Vaca);

            // HU-03: la vaca con su historial. LitrosProducidos sale de la propiedad
            // calculada de VacaDetalle, y Historial se convierte día por día.
            CreateMap<Vaca, VacaDetalleResponse>()
                .ForMember(d => d.LitrosProducidos, o => o.Ignore())
                .ForMember(d => d.Historial, o => o.Ignore());
            CreateMap<ProduccionLeche, ProduccionVacaResponse>();
            CreateMap<VacaDetalle, VacaDetalleResponse>()
                .IncludeMembers(x => x.Vaca);
        }
    }
}
