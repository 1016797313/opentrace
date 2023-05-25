using Eto.Drawing;
using Eto.Forms;
using System.Collections.ObjectModel;
using System;
using System.Diagnostics;
using System.Text.Json;
using System.IO;
using Resources = traceroute.Properties.Resources;

namespace traceroute
{
    class TracerouteResult
    {
        public TracerouteResult(string no, string ip, string time, string geolocation, string ASNumber, string hostname, string organization, string latitude, string longitude)
        {
            No = no;
            IP = ip;
            Time = time;
            Geolocation = geolocation;
            AS = ASNumber;
            Hostname = hostname;
            Organization = organization;
            Latitude = latitude;
            Longitude = longitude;
        }
        public string No { get; set; }
        public string IP { get; set; }
        public string Time { get; set; }
        public string Geolocation { get; set; }
        public string AS { get; set; }
        public string Hostname { get; set; }
        public string Organization { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
    }

    public partial class MainForm : Form
    {
        private ObservableCollection<TracerouteResult> tracerouteResultCollection = new ObservableCollection<TracerouteResult>();
        private static NextTraceWrapper CurrentInstance { get; set; }
        private static double gridSizePercentage = 0.5;
        private ComboBox HostInputBox;
        private GridView tracerouteGridView;
        private WebView mapWebView;
        private DropDown dataProviderSelection;
        private DropDown protocolSelection;
        private Button startTracerouteButton;
        private bool gridResizing = false;
        private bool appForceExiting = false;

        public MainForm()
        {
            Title = Resources.APPTITLE;
            MinimumSize = new Size(860, 600);

            // �����˵���
            var newWindowCommand = new Command { MenuText = Resources.NEW, ToolBarText = Resources.NEW_WINDOW_TEXT, Shortcut = Application.Instance.CommonModifier | Keys.N };
            newWindowCommand.Executed += (sender, e) =>
            {
                Process.Start(Process.GetCurrentProcess().MainModule.FileName);
            };

            var exportCommand = new Command { MenuText = Resources.EXPORT};

            var quitCommand = new Command { MenuText = Resources.QUIT, Shortcut = Application.Instance.CommonModifier | Keys.Q };
            quitCommand.Executed += (sender, e) => Application.Instance.Quit();

            var aboutCommand = new Command { MenuText = Resources.ABOUT };
            aboutCommand.Executed += (sender, e) => new AboutDialog().ShowDialog(this);

            var preferenceCommand = new Command { MenuText = Resources.PREFERENCES };
            preferenceCommand.Executed += (sender, e) => new PreferencesDialog().ShowModal();

            // �����˵���
            Menu = new MenuBar
            {
                Items =
                {
                    new SubMenuItem { Text = Resources.FILE, Items = {
                            newWindowCommand,
                            new SubMenuItem { Text = Resources.EXPORT_TO , Items = {
                                    new Command { MenuText = "HTML" },
                                    new Command { MenuText = "Plain text (CSV)" }
                            } },
                            preferenceCommand,
                            quitCommand
                        } },
                     new SubMenuItem { Text = Resources.HELP , Items = {
                             new SubMenuItem { Text = Resources.LANGUAGE , Items = {
                                    new Command { MenuText = "English" },
                                    new Command { MenuText = "��������" }
                            } },
                             aboutCommand
                         } }
                }
            };

            // �����ؼ�
            HostInputBox = new ComboBox { Text = "" };

            startTracerouteButton = new Button { Text = Resources.START };
            protocolSelection = new DropDown
            {
                Items = {
                    new ListItem{Text = "ICMP" ,Key= ""},
                    new ListItem{Text = "TCP",Key = "-T" },
                    new ListItem{Text = "UDP",Key = "-U" },
                },
                SelectedIndex = 0,
                ToolTip = Resources.PROTOCOL_FOR_TRACEROUTING
            };
            dataProviderSelection = new DropDown
            {
                Items = {
                    new ListItem{Text = "LeoMoeAPI" ,Key= ""},
                    new ListItem{Text = "IPInfo",Key = "--data-provider IPInfo" },
                    new ListItem{Text = "IP.SB",Key = "--data-provider IP.SB" },
                    new ListItem{Text = "Ip2region",Key = "--data-provider Ip2region" },
                    new ListItem{Text = "IPInsight" ,Key = "--data-provider IPInsight" },
                    new ListItem{Text = "IPAPI.com" ,Key = "--data-provider IPAPI.com" },
                    new ListItem{Text = "IPInfoLocal" ,Key = "--data-provider IPInfoLocal" },
                    new ListItem{Text = "CHUNZHEN" , Key = "--data-provider CHUNZHEN"}
                },
                SelectedIndex = 0,
                ToolTip = Resources.IP_GEO_DATA_PROVIDER
            };

            tracerouteGridView = new GridView { DataStore = tracerouteResultCollection };

            // ������Դ
            tracerouteGridView.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<TracerouteResult, string>(r => r.No) },
                HeaderText = "#"
            });
            tracerouteGridView.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<TracerouteResult, string>(r => r.IP) },
                HeaderText = "IP"
            });
            tracerouteGridView.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<TracerouteResult, string>(r => r.Time) },
                HeaderText = Resources.TIME_MS
            });
            /* �ϲ�λ�ú���Ӫ�̹���
            tracerouteGridView.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<TracerouteResult, string>(r => r.Geolocation + " " + r.Organization) },
                HeaderText = "Geolocation"
            });
            */
            tracerouteGridView.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<TracerouteResult, string>(r => r.Geolocation) },
                HeaderText = Resources.GEOLOCATION
            });
            tracerouteGridView.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<TracerouteResult, string>(r => r.Organization) },
                HeaderText = Resources.ORGANIZATION
            });
            tracerouteGridView.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<TracerouteResult, string>(r => r.AS) },
                HeaderText = "AS"
            });
            tracerouteGridView.Columns.Add(new GridColumn
            {
                DataCell = new TextBoxCell { Binding = Binding.Property<TracerouteResult, string>(r => r.Hostname) },
                HeaderText = Resources.HOSTNAME
            });

            mapWebView = new WebView
            {
                Url = new Uri("https://lbs.baidu.com/jsdemo/demo/webgl0_0.htm")
            };

            // �󶨿ؼ��¼���������ק�ı� GridView �߶ȡ�
            SizeChanged += MainForm_SizeChanged;
            MouseDown += Dragging_MouseDown;
            MouseUp += Dragging_MouseUp;
            MouseMove += MainForm_MouseMove;
            tracerouteGridView.MouseUp += Dragging_MouseUp;
            tracerouteGridView.SelectedRowsChanged += TracerouteGridView_SelectedRowsChanged;
            startTracerouteButton.Click += StartTracerouteButton_Click;
            HostInputBox.KeyUp += HostInputBox_KeyUp;

            // ʹ�� Table ���ִ���ҳ��
            var layout = new TableLayout
            {
                Padding = new Padding(10),
                Spacing = new Size(5, 5),
                Rows = {
                    new TableRow {
                        Cells = {
                        new TableLayout {
                            Spacing = new Size(10,10),
                            Rows =
                            {
                                new TableRow
                                {
                                    Cells =
                                    {
                                        new TableCell(HostInputBox,true),
                                        protocolSelection,
                                        dataProviderSelection,
                                        startTracerouteButton
                                    }
                                }
                            }
                        }
                    }
                    },
                    new TableRow {
                        Cells = {tracerouteGridView}
                    },
                    new TableRow{
                        Cells = {mapWebView}
                    },
                }
            };
            Content = layout;
        }

        private void HostInputBox_KeyUp(object sender, KeyEventArgs e)
        {
            if(e.Key == Keys.Enter)
            {
                if (CurrentInstance != null)  StopTraceroute();
                StartTracerouteButton_Click(sender, e);
            }
        }

        private void TracerouteGridView_SelectedRowsChanged(object sender, EventArgs e)
        {
            FocusMapPoint(tracerouteGridView.SelectedRow);
        }

        private void StartTracerouteButton_Click(object sender, EventArgs e)
        {
            if (CurrentInstance != null)
            {
                StopTraceroute();
                return;
            }
            tracerouteResultCollection.Clear(); // ���ԭ��GridView
            ResetMap(); // ���õ�ͼ
            try
            {
                var instance = new NextTraceWrapper(HostInputBox.Text + " --raw " + dataProviderSelection.SelectedKey);
                HostInputBox.Items.Add(new ListItem { Text = HostInputBox.Text });
                CurrentInstance = instance;
                startTracerouteButton.Text = Resources.STOP;
                instance.Output.CollectionChanged += (sender, e) =>
                {
                    if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                    {
                        Application.Instance.InvokeAsync(() =>
                        {
                            tracerouteResultCollection.Add((TracerouteResult)e.NewItems[0]);
                            UpdateMap((TracerouteResult)e.NewItems[0]);
                            tracerouteGridView.ScrollToRow(tracerouteResultCollection.Count - 1);
                        });
                    }
                };
                instance.OnAppQuit += (sender, e) =>
                {
                    Application.Instance.InvokeAsync(() =>
                    {
                        if(appForceExiting != true) { 
                            // ��������
                            startTracerouteButton.Text = Resources.START;
                            CurrentInstance = null;
                        }
                        else
                        {
                            // ǿ�ƽ���
                            appForceExiting = false;
                        }
                    });
                };
            } catch (FileNotFoundException)
            {
                DialogResult dr = MessageBox.Show(Resources.MISSING_COMP_TEXT,
                     Resources.MISSING_COMP, MessageBoxButtons.YesNo);
                if (dr == DialogResult.Yes)
                {
                    Process.Start(new ProcessStartInfo("https://mtr.moe/") { UseShellExecute = true });
                }
                return;
            }
        }
        private void StopTraceroute()
        {
            appForceExiting = true;
            CurrentInstance.Kill();
            startTracerouteButton.Text = Resources.START;
            CurrentInstance = null;
        }

        /*
         * ������ק���� GridView ��С
         */
        private void Dragging_MouseUp(object sender, MouseEventArgs e)
        {
            gridResizing = false;
            mapWebView.Enabled = true;
        }

        private void Dragging_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Location.Y >= tracerouteGridView.Bounds.Bottom + 15 && e.Location.Y <= tracerouteGridView.Bounds.Bottom + 20)
            {
                gridResizing = true;
                mapWebView.Enabled = false;
            }

        }

        private void MainForm_MouseMove(object sender, MouseEventArgs e)
        {
            // �������ָ��
            if (e.Location.Y >= tracerouteGridView.Bounds.Bottom + 15 && e.Location.Y <= tracerouteGridView.Bounds.Bottom + 20)
            {
                this.Cursor = Cursors.SizeBottom;
            }
            else
            {
                this.Cursor = Cursors.Default;
            }

            if (e.Buttons == MouseButtons.Primary && gridResizing)
            {
                if ((int)e.Location.Y > (tracerouteGridView.Bounds.Top + 100)) // ��С����Ϊ100px
                {

                    tracerouteGridView.Height = (int)e.Location.Y - tracerouteGridView.Bounds.Top - 15;
                    gridSizePercentage = (double)tracerouteGridView.Height / (Height - 75); // �������
                }
            }
        }

        private void MainForm_SizeChanged(object sender, EventArgs e)
        {
            int gridHeight;
            int totalHeight = this.Height - 75; // ��ȥ�߾��������ı����75px
            gridHeight = (int)(totalHeight * gridSizePercentage);
            tracerouteGridView.Height = gridHeight; // ��������ԭ�߶�
        }
        private void UpdateMap(TracerouteResult result)
        {
            // �� Result ת��Ϊ JSON
            string resultJson = JsonSerializer.Serialize(result);
            // ͨ�� ExecuteScriptAsync �ѽ������ȥ
            mapWebView.ExecuteScriptAsync(@"window.opentrace.addHop(`" + resultJson + "`);");
        }
        private void FocusMapPoint(int hopNo)
        {
            mapWebView.ExecuteScriptAsync(@"window.opentrace.focusHop(" + hopNo + ");");
        }
        private void ResetMap()
        {
            // ���û��߳�ʼ����ͼ
            mapWebView.ExecuteScriptAsync(traceroute.Properties.Resources.baiduMap);
        }
    }
}
