// RUTA SUGERIDA: Models/Helpers/DataReaderExt.cs
using System;
using System.Data;

namespace GraciaDivina
{
    /// <summary>
    /// Extensiones únicas y compartidas para leer columnas de forma segura.
    /// Mantener UNA sola definición en todo el proyecto.
    /// </summary>
    public static class DataReaderExt
    {
        /// <summary>
        /// ¿Existe una columna con este nombre?
        /// </summary>
        public static bool ColumnExists(this IDataRecord dr, string name)
        {
            for (int i = 0; i < dr.FieldCount; i++)
                if (dr.GetName(i).Equals(name, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        /// <summary>
        /// ¿Existe columna en este índice?
        /// </summary>
        public static bool ColumnExists(this IDataRecord dr, int index)
            => index >= 0 && index < dr.FieldCount;
    }
}
