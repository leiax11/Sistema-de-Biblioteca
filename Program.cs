using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SistemaBiblioteca
{
    public class Libro
    {
        public string ISBN { get; set; }
        public string Titulo { get; set; }
        public string Autor { get; set; }
        public string Genero { get; set; }
        public int CantidadDisponible { get; set; }

        public Libro(string isbn = "", string titulo = "", string autor = "", string genero = "", int cantidad = 0)
        {
            ISBN = isbn;
            Titulo = titulo;
            Autor = autor;
            Genero = genero;
            CantidadDisponible = cantidad;
        }
    }

    public class Prestamo
    {
        public string ISBN { get; set; }
        public string Usuario { get; set; }
        public DateTime Fecha { get; set; }  // Cambiado a DateTime para mejor manejo

        public Prestamo(string isbn = "", string usuario = "", DateTime? fecha = null)
        {
            ISBN = isbn;
            Usuario = usuario;
            Fecha = fecha ?? DateTime.Today;
        }
    }

    public class Biblioteca
    {
        private Dictionary<string, Libro> libros = new Dictionary<string, Libro>();
        private List<Prestamo> prestamos = new List<Prestamo>();
        private readonly string librosArchivo = Path.Combine(Directory.GetCurrentDirectory(), "Data", "libros.json");
        private readonly string prestamosArchivo = Path.Combine(Directory.GetCurrentDirectory(), "Data", "prestamos.json");

        // Configuración de encoding para soportar caracteres especiales
        static Biblioteca()
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;
        }

        #region Métodos de ayuda para entrada de datos
        private static string LeerCadena(string mensaje, bool obligatorio = true)
        {
            while (true)
            {
                Console.Write(mensaje);
                string input = Console.ReadLine()?.Trim() ?? string.Empty;

                if (!obligatorio || !string.IsNullOrWhiteSpace(input))
                    return input;

                Console.WriteLine("Este campo es obligatorio. Por favor ingrese un valor.");
            }
        }

        private static int LeerEntero(string mensaje, int min = int.MinValue, int max = int.MaxValue)
        {
            while (true)
            {
                Console.Write(mensaje);
                if (int.TryParse(Console.ReadLine(), out int resultado) && resultado >= min && resultado <= max)
                    return resultado;

                Console.WriteLine($"Por favor ingrese un número válido entre {min} y {max}.");
            }
        }

        private static DateTime LeerFecha(string mensaje)
        {
            while (true)
            {
                Console.Write(mensaje);
                if (DateTime.TryParseExact(Console.ReadLine(), "yyyy-MM-dd", 
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fecha))
                    return fecha;

                Console.WriteLine("Formato de fecha inválido. Use YYYY-MM-DD.");
            }
        }
        #endregion

        #region Persistencia de datos
        private void GuardarLibros()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(librosArchivo));
                var jObject = new JObject();
                foreach (var libro in libros.Values)
                {
                    jObject[libro.ISBN] = JObject.FromObject(new
                    {
                        titulo = libro.Titulo,
                        autor = libro.Autor,
                        genero = libro.Genero,
                        cantidad = libro.CantidadDisponible
                    });
                }
                File.WriteAllText(librosArchivo, jObject.ToString(Formatting.Indented));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al guardar libros: {ex.Message}");
            }
        }

        private void CargarLibros()
        {
            try
            {
                if (!File.Exists(librosArchivo)) return;

                string json = File.ReadAllText(librosArchivo);
                var jObject = JObject.Parse(json);

                libros = new Dictionary<string, Libro>();
                foreach (var item in jObject)
                {
                    libros[item.Key] = new Libro(
                        item.Key,
                        (string)item.Value["titulo"],
                        (string)item.Value["autor"],
                        (string)item.Value["genero"],
                        (int)item.Value["cantidad"]
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar libros: {ex.Message}");
                libros = new Dictionary<string, Libro>();
            }
        }

        private void GuardarPrestamos()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(prestamosArchivo));
                string json = JsonConvert.SerializeObject(prestamos.Select(p => new {
                    p.ISBN,
                    p.Usuario,
                    Fecha = p.Fecha.ToString("yyyy-MM-dd")
                }), Formatting.Indented);
                File.WriteAllText(prestamosArchivo, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al guardar préstamos: {ex.Message}");
            }
        }

        private void CargarPrestamos()
        {
            try
            {
                if (!File.Exists(prestamosArchivo)) return;

                string json = File.ReadAllText(prestamosArchivo);
                var prestamosData = JsonConvert.DeserializeObject<List<dynamic>>(json);
                
                prestamos = prestamosData?
                    .Select(p => new Prestamo(
                        (string)p.ISBN, 
                        (string)p.Usuario, 
                        DateTime.ParseExact((string)p.Fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture)))
                    .ToList() ?? new List<Prestamo>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar préstamos: {ex.Message}");
                prestamos = new List<Prestamo>();
            }
        }
        #endregion

        #region Funcionalidades principales
        public void AgregarLibro()
        {
            Console.WriteLine("\n--- Agregar Libro ---");
            
            string isbn = LeerCadena("ISBN: ");
            string titulo = LeerCadena("Título: ");
            string autor = LeerCadena("Autor: ");
            string genero = LeerCadena("Género: ");
            int cantidad = LeerEntero("Cantidad disponible: ", min: 0);

            libros[isbn] = new Libro(isbn, titulo, autor, genero, cantidad);
            GuardarLibros();
            Console.WriteLine("¡Libro agregado con éxito!");
        }

        public void MostrarLibros()
        {
            if (libros.Count == 0)
            {
                Console.WriteLine("\nNo hay libros registrados.");
                return;
            }

            Console.WriteLine("\n--- Libros Disponibles ---");
            foreach (var libro in libros.Values.OrderBy(l => l.Titulo))
            {
                Console.WriteLine($"ISBN: {libro.ISBN} | Título: {libro.Titulo} | " +
                                $"Autor: {libro.Autor} | Género: {libro.Genero} | " +
                                $"Disponibles: {libro.CantidadDisponible}");
            }
        }

        public void RegistrarPrestamo()
        {
            Console.WriteLine("\n--- Registrar Préstamo ---");
            string isbn = LeerCadena("ISBN del libro: ");

            if (!libros.TryGetValue(isbn, out var libro))
            {
                Console.WriteLine("Error: Libro no encontrado.");
                return;
            }

            if (libro.CantidadDisponible <= 0)
            {
                Console.WriteLine("Error: No hay ejemplares disponibles.");
                return;
            }

            string usuario = LeerCadena("Nombre del usuario: ");
            DateTime fecha = LeerFecha("Fecha (YYYY-MM-DD): ");

            prestamos.Add(new Prestamo(isbn, usuario, fecha));
            libro.CantidadDisponible--;
            GuardarLibros();
            GuardarPrestamos();
            Console.WriteLine("¡Préstamo registrado con éxito!");
        }

        public void BuscarPorTitulo()
        {
            Console.WriteLine("\n--- Buscar por Título ---");
            string termino = LeerCadena("Ingrese término de búsqueda: ", obligatorio: false).ToLower();

            var resultados = libros.Values
                .Where(l => l.Titulo.ToLower().Contains(termino))
                .OrderBy(l => l.Titulo)
                .ToList();

            MostrarResultadosBusqueda(resultados, "título");
        }

        public void BuscarPorAutor()
        {
            Console.WriteLine("\n--- Buscar por Autor ---");
            string termino = LeerCadena("Ingrese nombre del autor: ", obligatorio: false).ToLower();

            var resultados = libros.Values
                .Where(l => l.Autor.ToLower().Contains(termino))
                .OrderBy(l => l.Autor)
                .ToList();

            MostrarResultadosBusqueda(resultados, "autor");
        }

        public void BuscarPorGenero()
        {
            Console.WriteLine("\n--- Buscar por Género ---");
            string termino = LeerCadena("Ingrese género: ", obligatorio: false).ToLower();

            var resultados = libros.Values
                .Where(l => l.Genero.ToLower().Contains(termino))
                .OrderBy(l => l.Genero)
                .ToList();

            MostrarResultadosBusqueda(resultados, "género");
        }

        private void MostrarResultadosBusqueda(List<Libro> resultados, string criterio)
        {
            if (resultados.Count == 0)
            {
                Console.WriteLine($"No se encontraron libros con ese {criterio}.");
                return;
            }

            Console.WriteLine($"\nResultados de búsqueda ({resultados.Count} encontrados):");
            foreach (var libro in resultados)
            {
                Console.WriteLine($"ISBN: {libro.ISBN} | Título: {libro.Titulo} | " +
                                $"Autor: {libro.Autor} | Género: {libro.Genero} | " +
                                $"Disponibles: {libro.CantidadDisponible}");
            }
        }
        #endregion
    }

    class Program
    {
        static void Main(string[] args)
        {
            Biblioteca biblioteca = new Biblioteca();
            
            while (true)
            {
                Console.Clear();
                MostrarMenu();
                
                int opcion = LeerEntero("Seleccione una opción: ", 1, 7);

                switch (opcion)
                {
                    case 1:
                        biblioteca.AgregarLibro();
                        break;
                    case 2:
                        biblioteca.MostrarLibros();
                        break;
                    case 3:
                        biblioteca.RegistrarPrestamo();
                        break;
                    case 4:
                        biblioteca.BuscarPorTitulo();
                        break;
                    case 5:
                        biblioteca.BuscarPorAutor();
                        break;
                    case 6:
                        biblioteca.BuscarPorGenero();
                        break;
                    case 7:
                        Console.WriteLine("Saliendo del sistema...");
                        return;
                }

                Console.WriteLine("\nPresione cualquier tecla para continuar...");
                Console.ReadKey();
            }
        }

        static void MostrarMenu()
        {
            Console.WriteLine("=== Sistema de Gestión de Biblioteca ===");
            Console.WriteLine("1. Agregar libro");
            Console.WriteLine("2. Mostrar todos los libros");
            Console.WriteLine("3. Registrar préstamo");
            Console.WriteLine("4. Buscar por título");
            Console.WriteLine("5. Buscar por autor");
            Console.WriteLine("6. Buscar por género");
            Console.WriteLine("7. Salir");
        }

        static int LeerEntero(string mensaje, int min, int max)
        {
            while (true)
            {
                Console.Write(mensaje);
                if (int.TryParse(Console.ReadLine(), out int resultado) && resultado >= min && resultado <= max)
                    return resultado;

                Console.WriteLine($"Por favor ingrese un número entre {min} y {max}.");
            }
        }
    }
}