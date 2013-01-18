using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DevExpress.ExpressApp.Updater
{
    public partial class AddParameters : Form
    {

        private string[] _Parameters;

        public AddParameters()
        {
            InitializeComponent();

            DialogResult = DialogResult.Cancel;
        }

        private void btnOK_Click( object sender, EventArgs e )
        {
            if (String.IsNullOrWhiteSpace(txtParameters.Text))
            {
                MessageBox.Show("Поле с параметрами не должно быть пустым.", "Ошибка", MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                txtParameters.Focus();
            }
            else
            {
                Parameters = txtParameters.Text.Split(new char[] {' '});
                DialogResult = DialogResult.OK;
            }
        }

        public string[] Parameters
        {
            get { return _Parameters; }
            set { _Parameters = value; }
        }
    }
}
