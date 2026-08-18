namespace VendedoresApi.SQL.Services
{
    public class ParametrosAppConfig
    {

       public static string GetParametros(string seeion, string key)
        {
            IConfigurationRoot builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", true, true).Build();



            var secParametros = builder.GetSection(seeion);
            var dato = secParametros[key];

            return dato;

        }


    }

}