using System;
using System.Drawing; // Necesario para Point, Size, Color
using System.Linq;    // Necesario para .FirstOrDefault()
using System.Xml;
using System.IO;
using System.Windows.Forms;

namespace Practica2GeneraciónDinámicaDeInterfaces
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
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

                GenerarRecursivo(doc.SelectNodes("/Formulario/Control"), this);
                CargarDatosDesdeXML();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Método Maestro Recursivo: Maneja Controles, Menús y Tablas.
        /// </summary>
        private void GenerarRecursivo(XmlNodeList nodos, object padre)
        {
            foreach (XmlNode nodo in nodos)
            {
                string tipo = nodo.Attributes["Tipo"].Value;
                string nombre = nodo.Attributes["Nombre"].Value;
                string texto = nodo.Attributes["Texto"]?.Value ?? "";
                int x = int.Parse(nodo.Attributes["X"].Value);
                int y = int.Parse(nodo.Attributes["Y"].Value);
                int w = int.Parse(nodo.Attributes["Ancho"].Value);
                int h = int.Parse(nodo.Attributes["Alto"].Value);

                object nuevoElemento = null;

                switch (tipo)
                {
                    case "Label":
                        nuevoElemento = new Label { Name = nombre, Text = texto, Location = new Point(x, y), Size = new Size(w, h) };
                        break;

                    case "Button":
                        Button btn = new Button { Name = nombre, Text = texto, Location = new Point(x, y), Size = new Size(w, h) };
                        btn.Click += BotonGenerico_Click;
                        nuevoElemento = btn;
                        break;

                    case "Panel":
                        nuevoElemento = new Panel { Name = nombre, Location = new Point(x, y), Size = new Size(w, h), BorderStyle = BorderStyle.FixedSingle };
                        break;

                    case "MenuStrip":
                        nuevoElemento = new MenuStrip { Name = nombre, Dock = DockStyle.Top };
                        break;

                    case "MenuItem":
                        ToolStripMenuItem item = new ToolStripMenuItem { Name = nombre, Text = texto };
                        item.Click += BotonGenerico_Click;
                        nuevoElemento = item;
                        break;

                    case "DataGridView":
                        nuevoElemento = new DataGridView { Name = nombre, Location = new Point(x, y), Size = new Size(w, h) };
                        break;

                    case "Columna":
                        if (nodo.Attributes["Opciones"] != null)
                        {
                            DataGridViewComboBoxColumn comboCol = new DataGridViewComboBoxColumn();
                            comboCol.Name = nombre;
                            comboCol.HeaderText = texto;
                            comboCol.Items.AddRange(nodo.Attributes["Opciones"].Value.Split(','));
                            nuevoElemento = comboCol;
                        }
                        else
                        {
                            DataGridViewTextBoxColumn textCol = new DataGridViewTextBoxColumn();
                            textCol.Name = nombre;
                            textCol.HeaderText = texto;
                            if (nombre == "colID")
                            {
                                textCol.ReadOnly = true;
                                textCol.DefaultCellStyle.BackColor = Color.LightGray;
                            }
                            nuevoElemento = textCol;
                        }
                        break;

                    case "TextBox":
                        TextBox txt = new TextBox { Name = nombre, Text = texto, Location = new Point(x, y), Size = new Size(w, h) };
                        if (nombre == "txtBuscar")
                        {
                            txt.TextChanged += new EventHandler(TxtBuscar_TextChanged);
                        }
                        nuevoElemento = txt;
                        break;

                    case "StatusStrip":
                        StatusStrip ss = new StatusStrip { Name = nombre, Dock = DockStyle.Bottom };
                        ss.Items.Add(new ToolStripStatusLabel { Name = "statusLabel", Text = "Listo." });
                        nuevoElemento = ss;
                        break;
                }

                // Añadir el nuevo elemento a su PADRE correspondiente
                if (nuevoElemento != null)
                {
                    if (padre is Control controlPadre && nuevoElemento is Control controlHijo)
                    {
                        controlPadre.Controls.Add(controlHijo);
                    }
                    else if (padre is MenuStrip menuPadre && nuevoElemento is ToolStripItem itemHijo)
                    {
                        menuPadre.Items.Add(itemHijo);
                    }
                    else if (padre is ToolStripMenuItem itemPadre && nuevoElemento is ToolStripItem subItem)
                    {
                        itemPadre.DropDownItems.Add(subItem);
                    }
                    else if (padre is DataGridView gridPadre && nuevoElemento is DataGridViewColumn colHija)
                    {
                        gridPadre.Columns.Add(colHija);
                    }

                    // RECURSIÓN
                    if (nodo.HasChildNodes)
                    {
                        GenerarRecursivo(nodo.SelectNodes("Control"), nuevoElemento);
                    }
                }
            }
        }

        /// <summary>
        /// Evento de clic genérico para todos los botones y menús.
        /// </summary>
        private void BotonGenerico_Click(object sender, EventArgs e)
        {
            string nombre = "";
            string texto = "";

            if (sender is Control c) { nombre = c.Name; texto = c.Text; }
            else if (sender is ToolStripItem i) { nombre = i.Name; texto = i.Text; }

            switch (nombre)
            {
                case "btnAnadir":
                    var grid = this.Controls.Find("gridDatos", true).FirstOrDefault() as DataGridView;
                    if (grid != null)
                    {
                        int nuevoId = 1;
                        var filasConDatos = grid.Rows.Cast<DataGridViewRow>()
                            .Where(r => !r.IsNewRow && r.Cells["colID"].Value != null && int.TryParse(r.Cells["colID"].Value.ToString(), out _));

                        if (filasConDatos.Any())
                        {
                            int maxId = filasConDatos.Max(r => int.Parse(r.Cells["colID"].Value.ToString()));
                            nuevoId = maxId + 1;
                        }

                        grid.Rows.Add(nuevoId, "", "Usuario");
                        SetStatus($"Usuario {nuevoId} añadido.");
                    }
                    break;

                case "btnEditar":
                    var gridEdit = this.Controls.Find("gridDatos", true).FirstOrDefault() as DataGridView;
                    if (gridEdit != null && gridEdit.CurrentCell != null)
                    {
                        gridEdit.BeginEdit(true);
                    }
                    break;

                case "btnEliminar":
                    var gridDel = this.Controls.Find("gridDatos", true).FirstOrDefault() as DataGridView;
                    if (gridDel != null && gridDel.SelectedRows.Count > 0)
                    {
                        foreach (DataGridViewRow row in gridDel.SelectedRows)
                        {
                            if (!row.IsNewRow) gridDel.Rows.Remove(row);
                        }
                        SetStatus("Fila(s) eliminada(s).");
                    }
                    else
                    {
                        SetStatus("Selecciona una fila completa para eliminar.");
                    }
                    break;

                case "menuGuardar":
                    GuardarDatos();
                    SetStatus("Datos guardados.");
                    break;

                case "menuSalir":
                    Application.Exit();
                    break;

                case "menuAcercaDe":
                    MessageBox.Show("Práctica 2.2: Generador Dinámico de Interfaces\n...", "Acerca de", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;

                default:
                    SetStatus($"Acción '{texto}' ejecutada.");
                    break;
            }
        }

        // =====================================================================
        // MÉTODOS DE PERSISTENCIA (GUARDAR Y CARGAR)
        // =====================================================================

        private void CargarDatosDesdeXML()
        {
            string rutaDatos = Path.Combine(Application.StartupPath, "Datos.xml");
            if (!File.Exists(rutaDatos)) return;

            var grid = this.Controls.Find("gridDatos", true).FirstOrDefault() as DataGridView;
            if (grid == null) return;

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(rutaDatos);
                foreach (XmlNode nodoUsuario in doc.SelectNodes("/Datos/Usuario"))
                {
                    if (nodoUsuario.ChildNodes.Count >= 3)
                    {
                        grid.Rows.Add(
                            nodoUsuario.SelectSingleNode("ID")?.InnerText,
                            nodoUsuario.SelectSingleNode("Nombre")?.InnerText,
                            nodoUsuario.SelectSingleNode("Rol")?.InnerText
                        );
                    }
                }
                SetStatus("Datos cargados correctamente.");
            }
            catch (Exception ex) { SetStatus($"Error al cargar datos: {ex.Message}"); }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            GuardarDatos();
        }

        private void GuardarDatos()
        {
            var grid = this.Controls.Find("gridDatos", true).FirstOrDefault() as DataGridView;
            if (grid == null) return;

            try
            {
                XmlDocument doc = new XmlDocument();
                XmlNode docNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
                doc.AppendChild(docNode);

                XmlNode rootNode = doc.CreateElement("Datos");
                doc.AppendChild(rootNode);

                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row.IsNewRow) continue;

                    XmlNode userNode = doc.CreateElement("Usuario");
                    rootNode.AppendChild(userNode);

                    userNode.AppendChild(doc.CreateElement("ID")).InnerText = row.Cells["colID"].Value?.ToString() ?? "0";
                    userNode.AppendChild(doc.CreateElement("Nombre")).InnerText = row.Cells["colNombre"].Value?.ToString() ?? "";
                    userNode.AppendChild(doc.CreateElement("Rol")).InnerText = row.Cells["colRol"].Value?.ToString() ?? "Usuario";
                }

                string rutaDatos = Path.Combine(Application.StartupPath, "Datos.xml");
                doc.Save(rutaDatos);
            }
            catch (Exception ex)
            {
                SetStatus($"Error al guardar: {ex.Message}");
            }
        }

        // =====================================================================
        // MÉTODOS DE FILTRO Y BARRA DE ESTADO
        // =====================================================================

        /// <summary>
        /// *** MÉTODO CORREGIDO ***
        /// Evento que se dispara cada vez que el usuario escribe en el TextBox "txtBuscar".
        /// </summary>
        private void TxtBuscar_TextChanged(object sender, EventArgs e)
        {
            var grid = this.Controls.Find("gridDatos", true).FirstOrDefault() as DataGridView;
            var txt = sender as TextBox;
            if (grid == null || txt == null) return;

            string filtro = txt.Text.ToLower();

            // Recorremos las filas y ajustamos su visibilidad.
            // No necesitamos CurrencyManager.
            foreach (DataGridViewRow row in grid.Rows)
            {
                // Ignoramos la fila "nueva" que está al final
                if (row.IsNewRow) continue;

                // Obtenemos el valor de la celda "Nombre"
                var cellValue = row.Cells["colNombre"].Value?.ToString() ?? "";

                // Si el filtro está vacío O el valor de la celda contiene el filtro...
                if (string.IsNullOrEmpty(filtro) || cellValue.ToLower().Contains(filtro))
                {
                    row.Visible = true; // La mostramos
                }
                else
                {
                    row.Visible = false; // La ocultamos
                }
            }

            SetStatus($"Filtrando por: '{filtro}'");
        }


        /// <summary>
        /// Método de ayuda para escribir en la barra de estado.
        /// </summary>
        private void SetStatus(string message)
        {
            var statusBar = this.Controls.Find("statusBar", true).FirstOrDefault() as StatusStrip;
            var statusLabel = statusBar?.Items.Find("statusLabel", true).FirstOrDefault() as ToolStripStatusLabel;

            if (statusLabel != null)
            {
                statusLabel.Text = message;
            }
        }
    }
}