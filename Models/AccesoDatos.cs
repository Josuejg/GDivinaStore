using Microsoft.Data.SqlClient;
using System.Data;
using System;
namespace GraciaDivina.Models
{
    public class AccesoDatos
    {
        private readonly string _cn;

        public AccesoDatos(IConfiguration config)
        {
            _cn = config.GetConnectionString("DefaultConnection")
   ?? config.GetConnectionString("Conexion")
   ?? throw new InvalidOperationException(
        "Falta ConnectionStrings:DefaultConnection (o 'Conexion') en appsettings.json");

        }

        public async Task<int> EjecutarAsync(string sp, Action<SqlCommand>? parametros = null)
        {
            using var cn = new SqlConnection(_cn);
            using var cmd = new SqlCommand(sp, cn) { CommandType = CommandType.StoredProcedure };
            parametros?.Invoke(cmd);
            await cn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync();
        }

        public async Task<object?> EscalarAsync(string sp, Action<SqlCommand>? parametros = null)
        {
            using var cn = new SqlConnection(_cn);
            using var cmd = new SqlCommand(sp, cn) { CommandType = CommandType.StoredProcedure };
            parametros?.Invoke(cmd);
            await cn.OpenAsync();
            return await cmd.ExecuteScalarAsync();
        }
        public async Task<T> EscalarAsync<T>(string sp, Action<SqlCommand> parametros)
        {
            var obj = await EscalarAsync(sp, parametros); // tu método existente
            return (T)Convert.ChangeType(obj, typeof(T));
        }


        public async Task<List<T>> ConsultarAsync<T>(string sp, Func<IDataReader, T> map, Action<SqlCommand>? parametros = null)
        {
            var lista = new List<T>();
            using var cn = new SqlConnection(_cn);
            using var cmd = new SqlCommand(sp, cn) { CommandType = CommandType.StoredProcedure };
            parametros?.Invoke(cmd);
            await cn.OpenAsync();
            using var dr = await cmd.ExecuteReaderAsync();
            while (await dr.ReadAsync()) lista.Add(map(dr));
            return lista;
        }

        public async Task<T?> ConsultarUnoAsync<T>(string sp, Func<IDataReader, T> map, Action<SqlCommand>? parametros = null)
        {
            using var cn = new SqlConnection(_cn);
            using var cmd = new SqlCommand(sp, cn) { CommandType = CommandType.StoredProcedure };
            parametros?.Invoke(cmd);
            await cn.OpenAsync();
            using var dr = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow);
            if (await dr.ReadAsync()) return map(dr);
            return default;
        }
    }
}
