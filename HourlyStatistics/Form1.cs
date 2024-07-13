﻿using System;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System.Xml;

namespace HourlyStatistics
{
    public partial class Ui : Form
    {
        bool search = false;
        public Ui()
        {
            InitializeComponent();
            date.Text = DateTime.Now.AddDays(-1).ToString("d.M.yyyy");
            dataGridView1.Rows.Clear();
        }

        void Ui_Shown(object sender, EventArgs e)
        {
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"\settings.xml"))
            {
                new Thread(() =>
                {
                    XmlDocument xDoc = new XmlDocument();
                    xDoc.Load(AppDomain.CurrentDomain.BaseDirectory + @"\settings.xml");
                    XmlElement xRoot = xDoc.DocumentElement;
                    if (xRoot != null)
                    {
                        foreach (XmlElement xnode in xRoot)
                        {
                            if (xnode.Name == "ip")
                            {
                                ip.Text = xnode.InnerText;
                            }
                            if (xnode.Name == "date")
                            {
                                date.Text = xnode.InnerText;
                                continue;
                            }
                            getFactor();
                        }
                        search = true;
                    }
                }).Start();
            }
            else
            {
                search = true;
            }
        }

        void getFactor()
        {
            //new Thread(() =>
            //{
                ip.Enabled = false;
                date.Enabled = false;
                request.Enabled = false;
                clear.Enabled = false;
                save.Enabled = false;
                progressBar1.Value = 0;

                DateTime scheduleDate;
                if (DateTime.TryParseExact(date.Text, "d.M.yyyy", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out scheduleDate))
                {
                    DateTime myDate = DateTime.ParseExact(date.Text + " 00:00:00", "d.M.yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                    if (myDate < DateTime.Now)
                    {
                        string getDate = myDate.ToString("yyyy-MM-dd");
                        Match match = Regex.Match(ip.Text, @"\b((([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])(\.)){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5]))\b");
                        if (match.Success)
                        {
                            PingReply pr = new Ping().Send(ip.Text, 5000);
                            if (pr.Status == IPStatus.Success)
                            {
                                int rowNumbe = dataGridView1.Rows.Add();
                                dataGridView1.FirstDisplayedScrollingRowIndex = rowNumbe;
                                try
                                {
                                    HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create($"http://{ip.Text}/unitinfo/api/unitinfo");
                                    HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                                    using (StreamReader stream = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                                    {
                                        string factorJson = stream.ReadToEnd();
                                        var datajson = new JavaScriptSerializer().Deserialize<dynamic>(factorJson);
                                        string factoryNumber = datajson["unit"]["factoryNumber"];
                                        string serialNumber = datajson["certificate"]["serialNumber"];
                                        dataGridView1.Rows[rowNumbe].Cells[0].Value = serialNumber + " " + factoryNumber + " " + ip.Text;
                                        dataGridView1.Rows[rowNumbe].Cells[1].Value = myDate.ToString("dd-MM-yyyy");
                                    }
                                }
                                catch
                                {
                                    MessageBox.Show("Not a factor.", "IP unavailable.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }
                                string factorZ = "%2B03%3A00";
                                try
                                {
                                    HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create($"http://{ip.Text}/systemmanager/api/Time/timezones/current");
                                    HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                                    using (StreamReader stream = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                                    {
                                        string factorJson = stream.ReadToEnd();
                                        var datajson = new JavaScriptSerializer().Deserialize<dynamic>(factorJson);
                                        string factorZone = datajson["description"];
                                        factorZone = factorZone.Remove(factorZone.LastIndexOf(")"));
                                        factorZone = factorZone.Substring(factorZone.LastIndexOf("C") + 1);
                                        string zone = factorZone[0] == '+' ? "%2B" : "%2D";
                                        factorZone = factorZone.Substring(1);
                                        string factorH = factorZone.Remove(factorZone.LastIndexOf(":"));
                                        string factorM = factorZone.Substring(factorZone.LastIndexOf(":") + 1);
                                        factorZ = zone + factorH + "%3A" + factorM;
                                    }
                                }
                                catch
                                {
                                    MessageBox.Show("Not a factor.", "IP unavailable.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }
                                string strDate = getDate;
                                string endDate = getDate;
                                string str = "00";
                                string end = "01";


                                for (int i = 0; i < 24; i++)
                                {
                                    str = i.ToString("00");
                                    if (i < 23)
                                    {
                                        end = (i + 1).ToString("00");
                                    }
                                    else
                                    {
                                        DateTime newdate = DateTime.ParseExact(getDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                                        endDate = newdate.AddDays(1).ToString("yyyy-MM-dd");
                                        end = "00";
                                    }
                                    try
                                    {
                                        string url = $"http://{ip.Text}/archive/tracks/count?dateStart={strDate}T{str}%3A00%3A00{factorZ}&dateEnd={endDate}T{end}%3A00%3A00{factorZ}";
                                        HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(url);
                                        HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                                        using (StreamReader stream = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                                        {
                                            string factorJson = stream.ReadToEnd();
                                            var datajson = new JavaScriptSerializer().Deserialize<dynamic>(factorJson);
                                            dataGridView1.Rows[rowNumbe].Cells[2 + i].Value = datajson["count"];
                                            if (datajson["count"] < 1 ) 
                                            { 
                                                dataGridView1.Rows[rowNumbe].Cells[2 + i].Style.BackColor = Color.Red;
                                        }
                                            
                                            progressBar1.PerformStep();
                                        }
                                    }
                                    catch
                                    {
                                        MessageBox.Show("Factor request error not responding.", "Timed out.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                        progressBar1.PerformStep();
                                    }
                                }
                            }
                            else
                            {
                                if (search)
                                {
                                    MessageBox.Show("The specified IP address is unavailable.", "IP unavailable.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }
                            }
                        }
                        else
                        {
                            MessageBox.Show("Invalid IP address specified.", "Invalid IP.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    else
                    {
                        MessageBox.Show("The date must be correct.", "Invalid Date.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Invalid date format.", "Invalid Date.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                ip.Enabled = true;
                date.Enabled = true;
                request.Enabled = true;
                clear.Enabled = true;
                save.Enabled = true;
            //}).Start();
        }

        void request_Click(object sender, EventArgs e)
        {
            getFactor();
        }

        void clear_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();
        }

        void save_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = Application.StartupPath;
            saveFileDialog.Filter = "CSV|*.csv";
            saveFileDialog.FileName = "Statistics " + DateTime.Now.ToString("dd.MM.yyyy HH.mm");

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                FileInfo fil = new FileInfo(saveFileDialog.FileName);
                using (StreamWriter sw = fil.AppendText())
                {
                    var headers = dataGridView1.Columns.Cast<DataGridViewColumn>();
                    sw.WriteLine(string.Join(";", headers.Select(column => "\"" + column.HeaderText + "\"").ToArray()));
                    sw.Close();
                }
                using (StreamWriter sw = fil.AppendText())
                {
                    foreach (DataGridViewRow row in dataGridView1.Rows)
                    {
                        var cells = row.Cells.Cast<DataGridViewCell>();
                        sw.WriteLine(string.Join(";", cells.Select(cell => "\"" + cell.Value + "\"").ToArray()));
                    }
                    sw.Close();
                }
            }
        }

        void ip_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                getFactor();
            }
        }

        void date_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                getFactor();
            }
        }

        void Ui_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                getFactor();
            }
        }
    }
}
