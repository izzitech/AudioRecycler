using Npgsql;
using NpgsqlTypes;
using System;
using System.Data;
using System.IO;
using System.Text;

//-----------------------------------------------------------------------------------------------------------------------------------------------------
//
//      AudioRecycler
//      Archivador de audios generados por el InnovoInformePlayer.
//
//      Autor: Iván E. Sierra.
//      Fecha: 15 de Septiembre de 2014.
//
//-----------------------------------------------------------------------------------------------------------------------------------------------------

namespace AudioRecycler {
    public class Design {
        public static void PrintLineChar(char character) {
            for (int i = 0; i < Console.WindowWidth; i++) {
                Console.Write(character);
            }
        }
    }
    class Program {
        public static void PrintSplashScreen() {
            ConsoleColor colorOriginal = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.White;

            Design.PrintLineChar('=');
            Console.WriteLine("AudioRecicler: archivador de audios del InnovoInformes.");
            Design.PrintLineChar('=');
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine();
            Console.WriteLine("Bienvenido al archivador de audios del InnovoInformes.");
            Console.WriteLine("Este programa sirve para almacenar los audios antiguos, recuperar rendimiento y liberar espacio en disco.");
            Console.WriteLine();
        }

        private static NpgsqlConnection CargarParametrosGlobales() {
            ParametrosGlobales parametrosGlobales = new ParametrosGlobales();

            while (!File.Exists(VariablesGlobales._rutaConParametrosGlobales)) {
                Console.WriteLine("Intentamos crear el archivo ParametrosGlobales.xml...");
                parametrosGlobales.CrearParametrosGlobalesPorConsola("añsldfn83892i");
            }

            Console.WriteLine("Intentamos cargar el archivo ParametrosGlobales.xml...");
            return parametrosGlobales.LeerParametrosGlobales("añsldfn83892i");
        }

        private static DataTable ObtenerListaDeAudiosParaArchivar(NpgsqlConnection npgsqlConnection, DateTime fechaFinal) {
            NpgsqlDataAdapter da = new NpgsqlDataAdapter("audiosobtenerparaarchivar", npgsqlConnection);
            da.SelectCommand.CommandType = CommandType.StoredProcedure;

            da.SelectCommand.Parameters.Add("_fecha", NpgsqlDbType.Timestamp);
            da.SelectCommand.Parameters[0].Value = fechaFinal;

            DataTable audiosEscuchados = new DataTable();
            try {
                audiosEscuchados.BeginLoadData();
                da.Fill(audiosEscuchados);
                audiosEscuchados.EndLoadData();
                return audiosEscuchados;
            } catch {
                Console.WriteLine("Error al cargar la lista de audios desde la base de datos...");
                return null;
            }
        }

        private static void DeleteEmptyDirectories(string startLocation) {
            foreach (var directory in Directory.GetDirectories(startLocation)) {
                DeleteEmptyDirectories(directory);
                if (Directory.GetFiles(directory).Length == 0 &&
                    Directory.GetDirectories(directory).Length == 0) {
                    Directory.Delete(directory, false);
                }
            }
        }

        /// <summary>
        /// Mueve los audios de una carpeta a otra, asignandoles en la base de datos el estado "ARCHIVADO".
        /// </summary>
        /// <param name="npgsqlConnection"></param>
        /// <param name="dataTable"></param>
        private static void Mover(NpgsqlConnection npgsqlConnection, DataTable dataTable) {
            int id = 0;
            int movidos = 0;
            int warnings = 0;
            StringBuilder sbErrores = new StringBuilder();
            string rutaOrigen;
            string archivoDestino;

            for (int fila = 0; fila < dataTable.Rows.Count; fila++) {
                id = (int)dataTable.Rows[fila]["id"];
                rutaOrigen = (string)dataTable.Rows[fila]["ruta"];

                FileInfo archivoOrigen = new FileInfo(VariablesGlobales.UNC_Origen + rutaOrigen);
                System.Diagnostics.Debug.WriteLine("Ruta de origen del audio: " + archivoOrigen.FullName);

                FileInfo rutaDestino = new FileInfo(VariablesGlobales.UNC_Destino + rutaOrigen);
                archivoDestino = rutaDestino.DirectoryName + "\\" + id.ToString() + rutaDestino.Extension;
                System.Diagnostics.Debug.WriteLine("Ruta de destino del audio: " + archivoDestino);

                if (!Directory.Exists(archivoOrigen.DirectoryName) || !File.Exists(archivoOrigen.FullName)){
                    System.Diagnostics.Debug.WriteLine("El archivo no existe... salteando " + archivoOrigen.FullName);
                    continue;
                }

                    if (!Directory.Exists(rutaDestino.DirectoryName)) {
                        System.Diagnostics.Debug.WriteLine("La ruta de destino no existe, creando " + rutaDestino);
                        Directory.CreateDirectory(rutaDestino.DirectoryName);
                    }

                    File.Copy(archivoOrigen.FullName, archivoDestino);
                if (AudioArchivarEnDB(npgsqlConnection, id)){
                        File.Delete(archivoOrigen.FullName);
                    movidos += 1;
                    Console.WriteLine();
                    Console.WriteLine("Movido: " + archivoOrigen.FullName);
                    Console.WriteLine("A:      " + archivoDestino);
                }
            }

            DeleteEmptyDirectories(VariablesGlobales.UNC_Origen);

            Console.WriteLine();
            Design.PrintLineChar('-');
            Console.WriteLine("Se han almacenado {0} audios.", movidos);
            Design.PrintLineChar('-');
            Console.WriteLine();
            if (warnings > 0){
                Console.WriteLine("Han ocurrido {0} errores.", warnings.ToString());
            }
            Console.WriteLine();
        }

        private static bool AudioArchivarEnDB(NpgsqlConnection npgsqlConnection, int idAudioAArchivar) {
            NpgsqlCommand cmd = new NpgsqlCommand("audioarchivar", npgsqlConnection);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("_audioid", NpgsqlTypes.NpgsqlDbType.Integer);
            cmd.Parameters[0].Value = idAudioAArchivar;

                cmd.Connection.Open();
                cmd.ExecuteNonQuery();
                cmd.Connection.Close();
                return true;
        }

        private static void MostrarAyuda() {
            Design.PrintLineChar('-');
            Console.WriteLine("Los parametros que se pueden utilizar en este programa son los siguientes");
            Design.PrintLineChar('-');
            Console.WriteLine("-h             Este mensaje de ayuda.");
            Console.WriteLine("-cXXX          'XXX' = cantidad de columnas de la ventana de la consola.");
            Console.WriteLine("-rYYY          'YYY' = cantidad de filas de la ventana de la consola.");
            Console.WriteLine("-a             Cierra la aplicación automáticamente cuando concluye.");            
            Design.PrintLineChar('-');
        }

        static void Main(string[] args) {
            int columns = 120;
            int rows = 50;
            bool autoClose = false;

            NpgsqlConnection npgsqlConnection;
            DataTable listaDeAudios;

            // Revisamos los parámetros usados al llamar la aplicación
            for (int i = 0; i < args.Length; i++) {
                if (args[i].Length < 2) {
                    continue;
                }
                switch (args[i].Substring(0,2)) {
                    case "-h":
                        PrintSplashScreen();
                        MostrarAyuda();
                        Console.ReadKey();
                        return;
                    case "-c":
                        int columnsTemp;
                        int.TryParse(args[i].Substring(2), out columnsTemp);
                        columns = columnsTemp;
                        break;
                    case "-r":
                        int rowsTemp;
                        int.TryParse(args[i].Substring(2), out rowsTemp);
                        rows = rowsTemp;
                        break;
                    case "-a":
                        autoClose = true;
                        break;
                }
            }

            Console.SetWindowSize(columns, rows);
            PrintSplashScreen();

            npgsqlConnection = CargarParametrosGlobales();

            // Restamos al día de hoy la cantidad de días de gracia para saber a partir de que fecha empezar a borrar hacia el pasado.
            DateTime fechaFinal = DateTime.Today.AddDays(-VariablesGlobales.diasDeGracia);
            Console.WriteLine("Obteniendo audios escuchados de hasta hace {0} días, fecha final: {1}", VariablesGlobales.diasDeGracia, fechaFinal.ToString());
            listaDeAudios = ObtenerListaDeAudiosParaArchivar(npgsqlConnection, fechaFinal);

            // Movemos los audios de una carpeta a otra, reportando en la base de datos que el archivo fue "ARCHIVADO".
            if (listaDeAudios != null) {
                Mover(npgsqlConnection, listaDeAudios);
            } else {
                Console.WriteLine("La base de datos no ha devuelto resultados.");
            }

            if (!autoClose) {
                Console.ReadKey();
            }
        }
    }
}
