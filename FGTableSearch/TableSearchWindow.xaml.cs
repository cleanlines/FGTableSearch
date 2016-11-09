using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.ComponentModel;
using ESRI.ArcGIS.Desktop.AddIns;
using ESRI.ArcGIS.Framework;
using System.Data;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.CartoUI;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;

namespace FGTableSearch {
    /// <summary>
    /// Designer class of the dockable window add-in. It contains WPF user interfaces that
    /// make up the dockable window.
    /// </summary>
    public partial class TableSearchWindow : UserControl, INotifyPropertyChanged {

        public ObservableCollection<TableDropdownDataContext> Tables { get; set; } = new ObservableCollection<TableDropdownDataContext>();

        public event PropertyChangedEventHandler PropertyChanged;
        private TableHelper Helper { get; } = TableHelper.Instance;
        public DataTable topTable { get; set; } = new DataTable();
        private DataTable bottomTable { get; set; } = new DataTable();

        public TableSearchWindow() {
            InitializeComponent();
        }

        private void comboBox_DropDownOpened(object sender, EventArgs e) => NotifyPropertyChanged("Tables");
        public void NotifyPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        /// <summary>
        /// Implementation class of the dockable window add-in. It is responsible for 
        /// creating and disposing the user interface class of the dockable window.
        /// </summary>
        public class AddinImpl : ESRI.ArcGIS.Desktop.AddIns.DockableWindow {
            private System.Windows.Forms.Integration.ElementHost m_windowUI;

            public AddinImpl() {

            }

            protected override IntPtr OnCreateChild() {
                m_windowUI = new System.Windows.Forms.Integration.ElementHost();
                m_windowUI.Child = new TableSearchWindow();
                m_windowUI.VisibleChanged += (o, e) => {
                    LogLog.Instance.Logger[LogLogEnum.Debug]("OnCreateChild:VISIBILITYCHANGED");
                    var wpfform = ((System.Windows.Forms.Integration.ElementHost)o).Child;
                    var searchWindow = ArcMap.DockableWindowManager.GetDockableWindow(ThisAddIn.IDs.TableSearchWindow.ToUID());
                    LogLog.Instance.Logger[LogLogEnum.Debug]("Search Window:" + searchWindow.IsVisible().ToString());
                    if(!searchWindow.IsVisible()) ((TableSearchWindow)wpfform).Refresh();
                    //((System.Windows.Forms.Integration.ElementHost)o).Visible = !((System.Windows.Forms.Integration.ElementHost)o).Visible;
                };
                return m_windowUI.Handle;
            }

            protected override void Dispose(bool disposing) {
                if(m_windowUI != null)
                    m_windowUI.Dispose();

                base.Dispose(disposing);
            }
        }

        private void SetProgress(bool play) {
            IStatusBar statusBar = ArcMap.Application.StatusBar as IStatusBar;
            IAnimationProgressor animationProgressor = statusBar.ProgressAnimation;
            if(play) {
                animationProgressor.Show();
                animationProgressor.Play(0, -1, -1);
                statusBar.set_Message(0, "Processing Related Tables");
            } else {
                animationProgressor.Stop();
                animationProgressor.Hide();
                statusBar.set_Message(0, null);
            }
        }

        public void Refresh() {
            SetProgress(true);
            Tables.Clear();
            Helper.Refresh();
            foreach(var item in Helper) {
                Tables.Add(item);
            }
            SetProgress(false);
        }

        private void OnVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e) {
            //Debug.WriteLine("OnVisibilityChanged!");
            SetProgress(true);
            foreach(var item in Helper) Tables.Add(item);
            NotifyPropertyChanged("Tables");
            SetProgress(false);
        }

        private void OnWindowsUnloaded(object sender, RoutedEventArgs e) {
            //Debug.WriteLine("OnWindowsUnloaded!");
            SetProgress(true);
            foreach(var item in Helper) Tables.Add(item);
            NotifyPropertyChanged("Tables");
            SetProgress(false);
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e) {
            // TODO: loading animation
            foreach(var item in Helper) Tables.Add(item);
            NotifyPropertyChanged("Tables");
            Refresh();
            TableSearchButton butt = AddIn.FromID<TableSearchButton>(ThisAddIn.IDs.TableSearchButton);
            // we can't seem to get a handle on the open event
            butt.VisibilityChangedEvent += (o, evt) => {
                IDockableWindow searchWindow = ArcMap.DockableWindowManager.GetDockableWindow(ThisAddIn.IDs.TableSearchWindow.ToUID());
                if(searchWindow.IsVisible()) {
                    Refresh();
                }
            };
        }

        private void DoSearch() {
            if(String.IsNullOrEmpty(textBox.Text) || String.IsNullOrWhiteSpace(textBox.Text)) {
                return;
            }
            SetProgress(true);
            ClearTables();
            ResetColumns((TableDropdownDataContext)comboBox.SelectedItem);
            try {
                SearchUtility.Search(textBox.Text, (TableDropdownDataContext)comboBox.SelectedItem, comboBox1.SelectedValue.ToString()).ForEach(x => topTable.Rows.Add(x));
            } catch(Exception e) {
                Debug.WriteLine(e.Message);
            } finally {
                SetProgress(false);
            }
        }

        private void OnComboBoxKeyDown(object sender, KeyEventArgs e) {
            if(e.Key == Key.Return) {
                DoSearch();
            }
        }

        private void OnComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e) {
            //Debug.WriteLine("OnComboBoxSelectionChanged");
            comboBox1.ItemsSource = null;
            Clear();
            if(e.AddedItems.Count > 0) {
                comboBox1.ItemsSource = ((TableDropdownDataContext)e.AddedItems[0]).Fields;
                ResetColumns((TableDropdownDataContext)e.AddedItems[0]);
            }
        }

        private void ResetColumns(TableDropdownDataContext context) {
            if(context != null) {
                context.Fields.ForEach(x => topTable.Columns.Add(x));
                dataGrid.ItemsSource = null; //reset the binding - TODO get the xaml going
                dataGrid.ItemsSource = topTable.DefaultView;
            }
        }

        private void OnComboBox1SelectionChanged(object sender, SelectionChangedEventArgs e) {
            LogLog.Instance.Logger[LogLogEnum.Debug]("OnComboBox1SelectionChanged");
            LogLog.Instance.Logger[LogLogEnum.Debug](((System.Windows.Controls.ComboBox)sender).SelectedIndex.ToString());
        }

        private void OnRefreshButtonClick(object sender, RoutedEventArgs e) => Refresh();

        private void OnSearchButtonClick(object sender, RoutedEventArgs e) => DoSearch();


        private void OnDataGridSelectionChanged(object sender, SelectionChangedEventArgs e) {
            if(e.AddedItems.Count > 0) {
                var objarray = ((DataRowView)e.AddedItems[0]).Row.ItemArray;
                dataGrid1.ItemsSource = SearchUtility.GetRelatedFeatures((TableDropdownDataContext)comboBox.SelectedItem, objarray);
            }
        }

        private void Clear() {
            textBox.Clear();
            ClearTables();
        }
        private void ClearTables() {
            topTable.Columns.Clear();
            topTable.Rows.Clear();
            dataGrid.ItemsSource = topTable.DefaultView;
            bottomTable.Rows.Clear();
            dataGrid1.ItemsSource = bottomTable.DefaultView;
        }

        private void ZoomCommand(object sender, RoutedEventArgs e) {
            LogLog.Instance.Logger[LogLogEnum.Debug]("TableSearch:ZoomCommand");
            try {
                FeatureModel aFeat = dataGrid1.SelectedItem as FeatureModel;
                IActiveView av = ArcMap.Document.ActiveView as IActiveView;
                IGeometry aGeom = aFeat.Feature.Shape as IGeometry;
                LogLog.Instance.Logger[LogLogEnum.Debug]("Got feat, av and geom");
                if(aGeom != null) {
                    if(aGeom.GeometryType == esriGeometryType.esriGeometryPoint) {
                        IPoint pt = aGeom as IPoint;
                        var x = pt.X;
                        var y = pt.Y;
                        var borderAreaDistance = 100;
                        Envelope anEnv = new Envelope {
                            XMin = x - borderAreaDistance,
                            YMin = y - borderAreaDistance,
                            XMax = x + borderAreaDistance,
                            YMax = y + borderAreaDistance
                        };
                        av.Extent = anEnv as IEnvelope;
                        LogLog.Instance.Logger[LogLogEnum.Debug]("Reset av extent");
                    } else {
                        av.Extent = aFeat.Feature.Extent;
                    }
                }
                TableHelper th = TableHelper.Instance;
                LogLog.Instance.Logger[LogLogEnum.Debug]($"th:{th.ToString()}");
                ILayer l = th.GetLayerFromFeature(aFeat.Feature);
                LogLog.Instance.Logger[LogLogEnum.Debug]($"Layer:{l.ToString()}");
                TOCHelper tch = new TOCHelper();
                LogLog.Instance.Logger[LogLogEnum.Debug]($"Toc Helper:{tch.ToString()}");
                tch.MakeVisible(l);
                av.PartialRefresh(esriViewDrawPhase.esriViewAll, null, av.Extent);
            } catch(Exception ex) {
                LogLog.Instance.Logger[LogLogEnum.Error](ex.StackTrace.ToString());
                LogLog.Instance.Logger[LogLogEnum.Error](ex.Message);

            }
        }

        private void FlashCommand(object sender, RoutedEventArgs e) {
            LogLog.Instance.Logger[LogLogEnum.Debug]("TableSearch:FlashCommand");
            try {
                IActiveView av = ArcMap.Document.ActiveView as IActiveView;
                FeatureModel aFeat = dataGrid1.SelectedItem as FeatureModel;
                av.ScreenDisplay.UpdateWindow();
                IFeatureIdentifyObj featIdentify = new FeatureIdentifyObject();
                featIdentify.Feature = aFeat.Feature;
                IIdentifyObj identify = featIdentify as IIdentifyObj;
                identify.Flash(av.ScreenDisplay);
            } catch(Exception ex) {
                LogLog.Instance.Logger[LogLogEnum.Error](ex.Message);
            }
        }
        private void AddToSelectionCommand(object sender, RoutedEventArgs e) {
            LogLog.Instance.Logger[LogLogEnum.Debug]("TableSearch:AddToSelectionCommand");
            try {
                IActiveView av = ArcMap.Document.ActiveView as IActiveView;
                FeatureModel aFeat = dataGrid1.SelectedItem as FeatureModel;
                TableHelper th = TableHelper.Instance;
                ILayer l = th.GetLayerFromFeature(aFeat.Feature);
                if(!l.Visible) {
                    l.Visible = true;
                }
                av.FocusMap.SelectFeature(l, aFeat.Feature);
                av.PartialRefresh(esriViewDrawPhase.esriViewGeoSelection, null, null);
            } catch(Exception ex) {
                LogLog.Instance.Logger[LogLogEnum.Error](ex.Message);
            }
        }

        private void OnClearButtonClick(object sender, RoutedEventArgs e) => Clear();
    }
}
