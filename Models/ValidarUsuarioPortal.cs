namespace VendedoresApi.Models
{
    public class ValidarUsuarioPortal
    {
        public class ValidarUsuario
        {
            public int usuarioID {  get; set; }
            public string usuario { get; set; }
            public string clave { get; set; }
            public string CodError { get; set; }
            public string Mensaje { get; set; }
           

        }
    }
}
