using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace USBDeleter
{
    public partial class Form1 : Form
    {

        RegWork myReg;
        CancellationTokenSource cancelTokenSource = null;

        public Form1()
        {
            InitializeComponent();
            myReg = new RegWork(null, label1);
            label1.Text = "";
            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                checkedListBox1.SetItemChecked(i, true);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            listBox1.Items.AddRange(myReg.GetAllUsbDevices(checkBoxMobile.Checked));
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            listBox2.Items.Clear();
            listBox2.Items.AddRange(myReg.GetSerialOfCurrentUsb(listBox1.SelectedItem.ToString()));
        }


        async private void button2_Click(object sender, EventArgs e)
        {
            if (listBox2.SelectedItem == null) return;

            dataGridView1.Rows.Clear();

            cancelTokenSource = new CancellationTokenSource();
            CancellationToken token = cancelTokenSource.Token;
            try
            {
                string selItem = listBox2.SelectedItem.ToString();

                await Task.Run(() => { myReg.Find(token, selItem); });

                label1.Text = "Done !";
            }
            catch (OperationCanceledException)
            {
                label1.Text = "Canceled by User !";
            } 
            finally
            {
                cancelTokenSource.Dispose();

                foreach (var a in myReg.pathContent)
                {
                    if (myReg.pathContent[a.Key].Values.Count > 0)
                    {
                        foreach (var b in myReg.pathContent[a.Key])
                        {
                            dataGridView1.Rows.Add(a.Key, "", "");
                            dataGridView1.Rows[dataGridView1.RowCount - 2].Cells[1].Value = b.Key;
                            dataGridView1.Rows[dataGridView1.RowCount - 2].Cells[2].Value = b.Value;
                        }
                    }
                    else dataGridView1.Rows.Add(a.Key, "", "");
                }
            }     
            

        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null) return;

            if (myReg.DeleteSelectedFolderKeyValue(dataGridView1.CurrentRow.Cells[0].Value.ToString(),
                                                   dataGridView1.CurrentRow.Cells[1].Value.ToString(),
                                                   dataGridView1.CurrentRow.Cells[2].Value.ToString(),
                                                   checkBox1.Checked))
            {
                if (myReg.keyValDeleted)
                {
                    dataGridView1.CurrentRow.Cells[1].Value = string.Empty;
                    dataGridView1.CurrentRow.Cells[2].Value = string.Empty;
                }
                else if (myReg.pathDeleted)
                {
                    string deletedPath = dataGridView1.CurrentRow.Cells[0].Value.ToString();
                    for (int i = dataGridView1.Rows.Count - 2; i >= 0; i--)
                    {
                        if (dataGridView1.Rows[i].Cells[0].Value.ToString().Contains(deletedPath))
                        {
                            dataGridView1.Rows.RemoveAt(i);
                        }
                    }
                }
            } 
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (cancelTokenSource == null) return;
            cancelTokenSource.Cancel();
        }


        private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (checkedListBox1.SelectedItem!=null)
            {
                if (e.NewValue == CheckState.Unchecked)
                {
                    myReg.DeleteNotUsedKey(checkedListBox1.SelectedItem.ToString());
                }
            }
        }

        private void checkBoxMobile_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxMobile.Checked)
            {
                checkBoxUSB.Checked = false;
            }
            else
            {
                checkBoxUSB.Checked = true;
            }
        }

        private void checkBoxUSB_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxUSB.Checked)
            {
                checkBoxMobile.Checked = false;
            }
            else
            {
                checkBoxMobile.Checked = true;
            }
        }
    }
}
