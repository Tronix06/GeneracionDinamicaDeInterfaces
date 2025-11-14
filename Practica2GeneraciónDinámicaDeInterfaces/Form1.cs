using System.Xml;
using System.IO;
using System.Windows.Forms; // Necesario

namespace Practica2GeneraciónDinámicaDeInterfaces
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                string rutaXml = Path.Combine(Application.StartupPath, "Interfaz.xml");
                if (!File.Exists(rutaXml))
                {
                    MessageBox.Show("No se encontró Interfaz.xml");
                    return;
                }

                XmlDocument doc = new XmlDocument();
                doc.Load(rutaXml);

                // Iniciamos la recursión pasando 'this' (el formulario) como objeto padre
                GenerarRecursivo(doc.SelectNodes("/Formulario/Control"), this);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Método Maestro Recursivo: Maneja Controles, Menús y Tablas.
        /// Usamos 'object' para el padre porque un ToolStripMenuItem no es un Control.
        /// </summary>
        private void GenerarRecursivo(XmlNodeList nodos, object padre)
        {
            foreach (XmlNode nodo in nodos)
            {
                // 1. Leer atributos
                string tipo = nodo.Attributes["Tipo"].Value;
                string nombre = nodo.Attributes["Nombre"].Value;

                // Lectura segura de Texto
                string texto = "";
                if (nodo.Attributes["Texto"] != null) texto = nodo.Attributes["Texto"].Value;

                // Coordenadas (solo importan para controles normales)
                int x = int.Parse(nodo.Attributes["X"].Value);
                int y = int.Parse(nodo.Attributes["Y"].Value);
                int w = int.Parse(nodo.Attributes["Ancho"].Value);
                int h = int.Parse(nodo.Attributes["Alto"].Value);

                // Objeto que vamos a crear (puede ser Control, MenuItem o Columna)
                object nuevoElemento = null;

                // 2. Crear el objeto según el Tipo
                switch (tipo)
                {
                    case "Label":
                        Label lbl = new Label { Name = nombre, Text = texto, Location = new Point(x, y), Size = new Size(w, h) };
                        nuevoElemento = lbl;
                        break;

                    case "Button":
                        Button btn = new Button { Name = nombre, Text = texto, Location = new Point(x, y), Size = new Size(w, h) };
                        btn.Click += BotonGenerico_Click; // Añadimos evento
                        nuevoElemento = btn;
                        break;

                    case "Panel":
                        Panel pnl = new Panel { Name = nombre, Location = new Point(x, y), Size = new Size(w, h), BorderStyle = BorderStyle.FixedSingle };
                        nuevoElemento = pnl;
                        break;

                    // --- EXTENSIÓN MENÚ ---
                    case "MenuStrip":
                        MenuStrip ms = new MenuStrip { Name = nombre, Dock = DockStyle.Top };
                        nuevoElemento = ms;
                        break;

                    case "MenuItem":
                        ToolStripMenuItem item = new ToolStripMenuItem { Name = nombre, Text = texto };
                        item.Click += BotonGenerico_Click; // Los menús también responden al clic
                        nuevoElemento = item;
                        break;

                    // --- EXTENSIÓN TABLA ---
                    case "DataGridView":
                        DataGridView dgv = new DataGridView { Name = nombre, Location = new Point(x, y), Size = new Size(w, h) };
                        nuevoElemento = dgv;
                        break;

                    case "Columna":
                        // Verificamos si tiene el atributo especial "Opciones" (para el Rol)
                        if (nodo.Attributes["Opciones"] != null)
                        {
                            // Si tiene opciones, creamos una columna tipo ComboBox (Desplegable)
                            DataGridViewComboBoxColumn comboCol = new DataGridViewComboBoxColumn();
                            comboCol.Name = nombre;
                            comboCol.HeaderText = texto;

                            // Leemos las opciones separadas por coma y las añadimos
                            string[] opciones = nodo.Attributes["Opciones"].Value.Split(',');
                            comboCol.Items.AddRange(opciones);

                            nuevoElemento = comboCol;
                        }
                        else
                        {
                            // Si no, es una columna de texto normal
                            DataGridViewTextBoxColumn textCol = new DataGridViewTextBoxColumn();
                            textCol.Name = nombre;
                            textCol.HeaderText = texto;

                            // MEJORA: Si es la columna ID, la hacemos de solo lectura
                            if (nombre == "colID")
                            {
                                textCol.ReadOnly = true;
                                // Opcional: Ponerle un color de fondo gris para indicar que no se toca
                                textCol.DefaultCellStyle.BackColor = Color.LightGray;
                            }

                            nuevoElemento = textCol;
                        }
                        break;
                }

                // 3. Añadir el nuevo elemento a su PADRE correspondiente
                if (nuevoElemento != null)
                {
                    // Caso A: El padre es un Formulario o un Panel (Contenedores normales)
                    if (padre is Control controlPadre && nuevoElemento is Control controlHijo)
                    {
                        controlPadre.Controls.Add(controlHijo);
                    }
                    // Caso B: El padre es un MenuStrip (Añadimos ítems principales)
                    else if (padre is MenuStrip menuPadre && nuevoElemento is ToolStripItem itemHijo)
                    {
                        menuPadre.Items.Add(itemHijo);
                    }
                    // Caso C: El padre es un Ítem de Menú (Añadimos sub-menús)
                    else if (padre is ToolStripMenuItem itemPadre && nuevoElemento is ToolStripItem subItem)
                    {
                        itemPadre.DropDownItems.Add(subItem);
                    }
                    // Caso D: El padre es un DataGridView (Añadimos columnas)
                    else if (padre is DataGridView gridPadre && nuevoElemento is DataGridViewColumn colHija)
                    {
                        gridPadre.Columns.Add(colHija);
                    }

                    // 4. RECURSIÓN: Procesar los hijos de este elemento
                    if (nodo.HasChildNodes)
                    {
                        GenerarRecursivo(nodo.SelectNodes("Control"), nuevoElemento);
                    }
                }
            }
        }

        private void BotonGenerico_Click(object sender, EventArgs e)
        {
            // 1. Identificar QUÉ control/menú se pulsó (obtenemos su nombre y texto)
            string nombre = "";
            string texto = "";

            if (sender is Control c)
            {
                nombre = c.Name;
                texto = c.Text;
            }
            else if (sender is ToolStripItem i)
            {
                nombre = i.Name;
                texto = i.Text;
            }

            // 2. Decidir qué hacer usando el NOMBRE (el ID único del XML)
            switch (nombre)
            {
                // --- ACCIONES DE BOTONES DEL PANEL ---

                case "btnAnadir":
                    var grid = this.Controls.Find("gridDatos", true).FirstOrDefault() as DataGridView;
                    if (grid != null)
                    {
                        // 1. CALCULAR EL NUEVO ID AUTOMÁTICO
                        int nuevoId = 1; // Empezamos en 1 si la tabla está vacía

                        if (grid.Rows.Count > 0)
                        {
                            // Buscamos el ID más alto que exista actualmente en la columna 0
                            int maxId = 0;
                            foreach (DataGridViewRow row in grid.Rows)
                            {
                                // Verificamos que la celda tenga valor y no sea la fila nueva vacía
                                if (row.Cells["colID"].Value != null && int.TryParse(row.Cells["colID"].Value.ToString(), out int idActual))
                                {
                                    if (idActual > maxId) maxId = idActual;
                                }
                            }
                            nuevoId = maxId + 1;
                        }

                        // 2. AÑADIR LA FILA Y POBLAR LOS DATOS
                        int indiceFila = grid.Rows.Add();

                        // Asignamos el ID calculado (Celda 0)
                        grid.Rows[indiceFila].Cells["colID"].Value = nuevoId;

                        // Asignamos un Rol por defecto (Celda 2) para que no quede vacío
                        // "Usuario" es una de las opciones que pusimos en el XML
                        grid.Rows[indiceFila].Cells["colRol"].Value = "Usuario";
                    }
                    break;

                case "btnEditar":
                    var gridEdit = this.Controls.Find("gridDatos", true).FirstOrDefault() as DataGridView;
                    if (gridEdit != null && gridEdit.CurrentCell != null)
                    {
                        // Forzamos que la celda seleccionada actualmente entre en modo edición
                        gridEdit.BeginEdit(true);
                    }
                    break;

                case "btnEliminar":
                    var gridDel = this.Controls.Find("gridDatos", true).FirstOrDefault() as DataGridView;
                    if (gridDel != null && gridDel.SelectedRows.Count > 0)
                    {
                        // Eliminamos la fila que el usuario haya seleccionado
                        // (Se debe seleccionar la fila entera haciendo clic en la cabecera gris de la izquierda)
                        foreach (DataGridViewRow row in gridDel.SelectedRows)
                        {
                            if (!row.IsNewRow) // No intentamos borrar la fila "nueva" vacía
                            {
                                gridDel.Rows.Remove(row);
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Por favor, selecciona una fila completa (haz clic en el cabezal gris) para eliminarla.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    break;

                // --- ACCIONES DEL MENÚ ---

                case "menuSalir":
                    Application.Exit();
                    break;

                case "menuAcercaDe":
                    MessageBox.Show(
                        "Práctica 2.2: Generador Dinámico de Interfaces\n\n" +
                        "Creado con un parser XML recursivo que soporta:\n" +
                        "- Controles anidados (Paneles)\n" +
                        "- Menús (MenuStrip)\n" +
                        "- Tablas (DataGridView)\n",
                        "Acerca de",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    break;

                // --- ACCIÓN POR DEFECTO ---
                default:
                    // Si el botón no tiene una acción especial (ej. "Ayuda"), solo muestra su texto
                    MessageBox.Show($"Has pulsado: {texto}", "Acción");
                    break;
            }
        }
    }
}