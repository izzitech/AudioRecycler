using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;

//--------------------------------------------------------------------------------------------------------------------------------------------------------
//
//      AudioRecycler
//      Archivador de audios generados por el InnovoInformePlayer.
//
//      Autor: Iván E. Sierra.
//      Fecha: 15 de Septiembre de 2014.
//      Comentario: Esta pieza está basada en el generador de ParametrosGlobales de Innovo, pero altamente refactorizado, simplificado y un poco mejorado.
//
//--------------------------------------------------------------------------------------------------------------------------------------------------------

namespace AudioRecycler
{
    public class VariablesGlobales
    {
        // Almacenamos la ruta del XML de ParametrosGlobales
        public static string _rutaConParametrosGlobales = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location.ToString()) + @"\ParametrosGlobales.xml";
        public static string UNC_Origen;
        public static string UNC_Destino;
        public static int diasDeGracia;
    }

    [XmlRootAttribute("ParametrosGlobales", Namespace = "", IsNullable = false)]
    public class ParametrosGlobalesObjeto
    {
        // Valores de conexión a la base de datos.
        public string serverIp { get; set; }
        public int serverPuerto { get; set; }
        public string baseDeDatos { get; set; }
        public string schema { get; set; }
        public string iam { get; set; }
        public string verify { get; set; }
        
        // Ruta de la carpeta compartida que contiene los audios del InnovoInformePlayer.
        public string UNC_Origen { get; set; }
        // Ruta a donde se van a mover los audios.
        public string UNC_Destino { get; set; }

        // Cantidad de días de audios que se van a dejar sin archivar.
        public int diasSinArchivar { get; set; }
    }

    public class ParametrosGlobales
    {
        public bool CrearParametrosGlobalesPorConsola(string salt)
        {
            ParametrosGlobalesObjeto parametrosGlobalesObjeto = new ParametrosGlobalesObjeto();

            ConsoleColor colorOriginal = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
            Design.PrintLineChar('-');
            Console.WriteLine("Creacion del archivo ParametrosGlobales.xml");
            Design.PrintLineChar('-');
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine();
            Console.WriteLine("El archivo ParametrosGlobales.xml guarda la configuracion esencial del programa.");
            Console.WriteLine("Por favor ingrese los datos que se solicitan a continuación.");
            Console.WriteLine();

            // La razón de que estén separadas las strings de consulta con la asignación de las variables,
            // es dar la posibilidad de que tengan distinto color las preguntas de las respuestas sin tener
            // que repetir el mismo código una y otra vez.
            List<string> datos = new List<string>();
            datos.Add("Direccion IP: ");
            datos.Add("Puerto: ");
            datos.Add("Nombre de la base de datos: ");
            datos.Add("Schema de la base de datos: ");
            datos.Add("Usuario: ");
            datos.Add("Contraseña: ");
            datos.Add("Direccion UNC donde están los audios: ");
            datos.Add("Direccion UNC de destino: ");
            datos.Add("Cantidad de días a mantener: ");

            bool losDatosSonCorrectos = false;

            while (!losDatosSonCorrectos) {
                SolicitarDatos(ref parametrosGlobalesObjeto, datos, salt);
                losDatosSonCorrectos = ConsultarDatos(parametrosGlobalesObjeto, datos);
            }

            TextWriter xmlStream = new StreamWriter(VariablesGlobales._rutaConParametrosGlobales);
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ParametrosGlobalesObjeto));

            xmlSerializer.Serialize(xmlStream, parametrosGlobalesObjeto);
            xmlStream.Close();

            Console.WriteLine();
            Console.WriteLine("¡El archivo fue creado exitosamente!");
            Console.WriteLine();
            Console.ForegroundColor = colorOriginal;

            return true;
        }

        private void SolicitarDatos(ref ParametrosGlobalesObjeto parametrosGlobalesObjeto, List<string> datosASolicitar, string salt) {
            for (int i = 0; i < datosASolicitar.Count; i++) {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write(datosASolicitar[i]);
                Console.ForegroundColor = ConsoleColor.White;

                switch (i) {
                    case 0:
                        parametrosGlobalesObjeto.serverIp = Console.ReadLine();
                        break;
                    case 1:
                        int puertoTemp;
                        if (int.TryParse(Console.ReadLine(), out puertoTemp)) {
                            parametrosGlobalesObjeto.serverPuerto = puertoTemp;
                        } else {
                            Console.ForegroundColor = ConsoleColor.Gray;
                            Console.WriteLine("Los datos introducidos no pudieron ser almacenados... intente otra vez.");
                            i--;
                        }
                        break;
                    case 2:
                        parametrosGlobalesObjeto.baseDeDatos = Console.ReadLine();
                        break;
                    case 3:
                        parametrosGlobalesObjeto.schema = Console.ReadLine();
                        break;
                    case 4:
                        string iamTemp = Console.ReadLine();
                        parametrosGlobalesObjeto.iam = SSTCryptographer.Encrypt(iamTemp, parametrosGlobalesObjeto.baseDeDatos + salt);
                        break;
                    case 5:
                        string verifyTemp = Console.ReadLine();
                        parametrosGlobalesObjeto.verify = SSTCryptographer.Encrypt(verifyTemp, parametrosGlobalesObjeto.iam + salt);
                        break;
                    case 6:
                        parametrosGlobalesObjeto.UNC_Origen = Console.ReadLine();
                        break;
                    case 7:
                        parametrosGlobalesObjeto.UNC_Destino = Console.ReadLine();
                        break;
                    case 8:
                        int diasTemp;
                        if (int.TryParse(Console.ReadLine(), out diasTemp)) {
                            parametrosGlobalesObjeto.diasSinArchivar = diasTemp;
                        } else {
                            Console.ForegroundColor = ConsoleColor.Gray;
                            Console.WriteLine("Los datos introducidos no pudieron ser almacenados... intente otra vez.");
                            i--;
                        }
                        break;
                }
            }
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        private bool ConsultarDatos(ParametrosGlobalesObjeto parametrosGlobalesObjeto, List<string> datosAConsultar){
            Console.WriteLine();
            Console.WriteLine("Verifique los datos ingresados");
            Console.WriteLine("------------------------------");
            for (int i = 0; i < datosAConsultar.Count; i++) {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write(datosAConsultar[i]);
                Console.ForegroundColor = ConsoleColor.White;
                switch (i) {
                    case 0:
                        Console.Write(parametrosGlobalesObjeto.serverIp + "\n");
                        break;
                    case 1:
                        Console.Write(parametrosGlobalesObjeto.serverPuerto + "\n");
                        break;
                    case 2:
                        Console.Write(parametrosGlobalesObjeto.baseDeDatos + "\n");
                        break;
                    case 3:
                        Console.Write(parametrosGlobalesObjeto.schema + "\n");
                        break;
                    case 4:
                        Console.Write("******" + "\n");
                        break;
                    case 5:
                        Console.Write("******" + "\n");
                        break;
                    case 6:
                        Console.Write(parametrosGlobalesObjeto.UNC_Origen + "\n");
                        break;
                    case 7:
                        Console.Write(parametrosGlobalesObjeto.UNC_Destino + "\n");
                        break;
                    case 8:
                        Console.Write(parametrosGlobalesObjeto.diasSinArchivar.ToString() + "\n");
                        break;
                }
                Console.ForegroundColor = ConsoleColor.Gray;
            }

            while (true) {
                Console.WriteLine();
                Console.WriteLine("¿Son estos datos correctos? (S)í, (N)o: ");
                ConsoleKeyInfo respuesta = Console.ReadKey(true);
                Console.WriteLine();
                if (respuesta.Key == ConsoleKey.S) {
                    return true;
                } else if (respuesta.Key == ConsoleKey.N){
                    Console.WriteLine("Ingrese los datos nuevamente...");
                    Console.WriteLine("-------------------------------");
                    return false;
                }
            }
        }
        public NpgsqlConnection LeerParametrosGlobales(string SALT)
        {
            ParametrosGlobalesObjeto parametrosGlobalesObjeto = new ParametrosGlobalesObjeto();

            NpgsqlConnection npgsqlConnection = new NpgsqlConnection();
            StreamReader streamReader;

            if (File.Exists(VariablesGlobales._rutaConParametrosGlobales))
            {
                try
                {
                    streamReader = new StreamReader(VariablesGlobales._rutaConParametrosGlobales);
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(ParametrosGlobalesObjeto));
                    parametrosGlobalesObjeto = (ParametrosGlobalesObjeto)xmlSerializer.Deserialize(streamReader);
                    streamReader.Close();

                    npgsqlConnection.ConnectionString =
                                       "Server=" + parametrosGlobalesObjeto.serverIp + ";" +
                                       "Port=" + parametrosGlobalesObjeto.serverPuerto + ";" +
                                       "User Id=" + SSTCryptographer.Decrypt(parametrosGlobalesObjeto.iam, parametrosGlobalesObjeto.baseDeDatos + SALT) + ";" +
                                       "Password=" + SSTCryptographer.Decrypt(parametrosGlobalesObjeto.verify, parametrosGlobalesObjeto.iam + SALT) + ";" +
                                       "Database=" + parametrosGlobalesObjeto.baseDeDatos + ";" +
                                       "SearchPath='" + parametrosGlobalesObjeto.schema + "';";

                    VariablesGlobales.UNC_Origen = parametrosGlobalesObjeto.UNC_Origen;
                    VariablesGlobales.UNC_Destino = parametrosGlobalesObjeto.UNC_Destino;
                    VariablesGlobales.diasDeGracia = parametrosGlobalesObjeto.diasSinArchivar;

                    return npgsqlConnection;
                }
                catch
                {
                    Console.WriteLine("Error en la lectura de los parametros globales.");
                    Console.ReadKey();
                }
            }
            return null;
        }
    }
}
