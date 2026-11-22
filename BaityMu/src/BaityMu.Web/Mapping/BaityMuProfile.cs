using AutoMapper;

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
    /// Por ahora está vacío. Cada historia agrega aquí sus mapeos, por ejemplo en la HU-01:
    /// CreateMap&lt;Vaca, VacaResponse&gt;();
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
        }
    }
}
