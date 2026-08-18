using System.Data.SqlClient;

namespace VendedoresApi.SQL.Services
{
    public static class MultiEmpresaDb
    {
        public static string GetConnectionString(IConfiguration configuration)
        {
            return configuration.GetConnectionString("ConnectionMultiEmpresa") ?? "";
        }

        public static SqlConnection GetConnection(IConfiguration configuration)
        {
            return new SqlConnection(GetConnectionString(configuration));
        }
    }
}
