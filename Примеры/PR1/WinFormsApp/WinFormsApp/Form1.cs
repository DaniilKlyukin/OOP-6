using System.Reflection;

namespace WinFormsApp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // ���� � ������ �������
            // TODO: ����� ������� ����� �� ����������� ����
            string pluginPath = "��� ���� �� �����\\PlantsLib.dll";

            // ��������� ������ �������
            Assembly pluginAssembly = Assembly.LoadFrom(pluginPath);

            // TODO: ����� �������, ����� ������������ ��� ������� ����� �� ����
            Type type = pluginAssembly.GetType("PlantsLib.Plant");

            var xLabels = 25;
            var xTextBoxes = 175;

            var y0 = 50;
            var dy = 35;

            var propertiesDictionary = new Dictionary<string, TextBox>();

            // TODO: ������� � �����
            foreach (var property in type.GetProperties())
            {
                // TODO: ���������������� ��� ����� ����� �� ��������� � ��������
                var label = new Label()
                {
                    Text = property.Name,
                    Location = new Point(xLabels, y0),
                    AutoSize = true
                };

                var textBox = new TextBox()
                {
                    Location = new Point(xTextBoxes, y0)
                };

                propertiesDictionary.Add(property.Name, textBox);

                Controls.Add(label);
                Controls.Add(textBox);

                y0 += dy;
            }

            var confirmButton = new Button()
            {
                Text = "���������",
                Location = new Point(xTextBoxes, y0),
            };

            // TODO: ����� ������� ��� �� ������
            confirmButton.Click += (s, a) =>
            {
                object? obj = Activator.CreateInstance(type);

                foreach (var property in type.GetProperties())
                {
                    var textBox = propertiesDictionary[property.Name];

                    switch (property.PropertyType.Name.ToLower())
                    {
                        case "string":
                            {
                                property.SetValue(obj, textBox.Text);
                            }
                            break;
                        case "double":
                            {
                                var value = double.Parse(textBox.Text);
                                property.SetValue(obj, value);
                            }
                            break;
                        default:
                            break;
                    }
                }

                var jsonStr = JsonSerializer.Serialize(obj);

                jsonTextBox.Text = jsonStr;

                var sfd = new SaveFileDialog()
                {
                    AddExtension = true,
                    DefaultExt = ".json"
                };

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(sfd.FileName, jsonStr);
                }
            };

            Controls.Add(confirmButton);
        }
    }
}
