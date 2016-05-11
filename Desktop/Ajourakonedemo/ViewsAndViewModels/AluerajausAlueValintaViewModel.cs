using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ArcGISRuntime.Samples.DesktopViewer.Utils;
using Catel.Data;
using Catel.MVVM;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Layers;
using Esri.ArcGISRuntime.Tasks.Query;


namespace ArcGISRuntime.Samples.DesktopViewer.ViewsAndViewModels
{
    public class AluerajausAlueValintaViewModel : ViewModelBase
    {
        protected FeatureLayer CurrentLayer;

        #region properties
        /// <summary>
        /// Gets the title of the view model.
        /// </summary>
        /// <value>The title.</value>
        public override string Title { get { return "Alueet"; } }

        public IEnumerable<Feature> Alueet
        {
            get { return GetValue<IEnumerable<Feature>>(AlueetProperty); }
            private set { SetValue(AlueetProperty, value); }
        }
        public static readonly PropertyData AlueetProperty = RegisterProperty("Alueet", typeof(IEnumerable<Feature>));

        public Feature ValittuAlue
        {
          get { return GetValue<Feature>(ValittuAlueProperty); }
            private set { SetValue(ValittuAlueProperty, value); }
        }
        public static readonly PropertyData ValittuAlueProperty = RegisterProperty("ValittuAlue", typeof(Feature));


        public DataRow ValittuRivi
        {
            get { return GetValue<DataRow>(ValittuRiviProperty); }
            private set
            {
                UpdateValittuAlue(value);
                SetValue(ValittuRiviProperty, value);
            }
        }
        public static readonly PropertyData ValittuRiviProperty = RegisterProperty("ValittuRivi", typeof(DataRow));

        public DataTable Features
        {
            get { return GetValue<DataTable>(FeaturesProperty); }
            private set { SetValue(FeaturesProperty, value); }
        }
        public static readonly PropertyData FeaturesProperty = RegisterProperty("Features", typeof(DataTable));

        public bool Ok
        {
            get { return GetValue<bool>(OkProperty); }
            private set { SetValue(OkProperty, value); }
        }
        public static readonly PropertyData OkProperty = RegisterProperty("Ok", typeof(bool));

        public string ObjectIdField
        {
            get { return GetValue<string>(ObjectIdFieldProperty); }
            private set { SetValue(ObjectIdFieldProperty, value); }
        }
        public static readonly PropertyData ObjectIdFieldProperty = RegisterProperty("ObjectIDField", typeof(string));

        #endregion

        #region Commands

        public Command OkCommand { get; private set; }
        public Command SuljeCommand { get; private set; }
        public Command ZoomToGeometriesCommand { get; private set; }
       
        #endregion

        public AluerajausAlueValintaViewModel()
        {
            OkCommand = new Command(OnOk);
            SuljeCommand = new Command(OnSulje);
            ZoomToGeometriesCommand = new Command(OnZoomToGeometries, CanZoomToGeometry);
        }

        private bool CanZoomToGeometry()
        {
            return ValittuAlue != null; 
        }

        public async Task<AluerajausAlueValintaViewModel> InitASync(FeatureLayer layer, bool useSelectedFeature = false)
        {
            if (!layer.SelectedFeatureIDs.Any() || !useSelectedFeature)
            {
                var ft = layer.FeatureTable as ServiceFeatureTable;

                if (ft != null)
                {
                    var query = new Query("1=1") {ReturnGeometry = false, OutFields = OutFields.All };
                    var queryTask = new QueryTask(new Uri(ft.ServiceUri));
                   
                    var features = await queryTask.ExecuteAsync(query);
                    Alueet = new List<Feature>(features.FeatureSet.Features);
                }
                else
                {
                    var arcgisFt = layer.FeatureTable as ArcGISFeatureTable;
                    if (arcgisFt != null)
                    {
                        Alueet = await arcgisFt.QueryAsync(new QueryFilter() { WhereClause = "1=1", MaximumRows = 1000});
                    }
                    else
                    {
                        var shapeFileTable = layer.FeatureTable as ShapefileTable;
                        if (shapeFileTable != null)
                        {
                            Alueet = await shapeFileTable.QueryAsync(new QueryFilter() { WhereClause = "1=1", MaximumRows = 1000 });
                        }
                    }
                }

            }
            else
            {
                Alueet = await layer.FeatureTable.QueryAsync(layer.SelectedFeatureIDs);
            }

            CurrentLayer = layer;
            RaisePropertyChanged("ShowAvaaHanke");

            ObjectIdField = layer.FeatureTable.ObjectIDField;
            var attributes = Alueet.Select(feature => feature.Attributes).ToList();
            Features = ToDataTable(attributes, layer.FeatureTable.Schema.Fields.ToList());
            return this;
        }

        private DataTable ToDataTable(IReadOnlyCollection<IDictionary<string, object>> list, List<FieldInfo> fields)
        {
            var result = new DataTable();
            if (list.Count == 0)
                return result;

            var columnNames = list.SelectMany(dict => dict.Keys).Distinct();
            
            foreach (var columnName in columnNames)
            {
                DataColumn dataColumn = new DataColumn(columnName);

                var field = fields.FirstOrDefault(o => String.Equals(o.Name, columnName, StringComparison.CurrentCultureIgnoreCase));
                if (field != null && field.Type != FieldType.GlobalID && field.Type != FieldType.Guid)
                {
                    result.Columns.Add(dataColumn);
                }

            }

            result.Columns[ObjectIdField].SetOrdinal(0);

            foreach (var item in list)
            {
                var row = result.NewRow();
                foreach (var key in item.Keys)
                {
   
                    var field = fields.FirstOrDefault(o => o.Name == key);
                    if (field == null) continue;
                    var value = ParseValue(item[key], field.Type);
                    if (field.Type != FieldType.GlobalID && field.Type != FieldType.Guid)
                    {
                        row[key] = value;
                }
                }
                
                result.Rows.Add(row);
            }

            return result;
        }

        private object ParseValue(object valueToParse, FieldType valueType)
        {
            try
            {
                switch (valueType)
                {
                    case FieldType.Date:
                        if (valueToParse == null)
                        {
                            valueToParse = "";
                        }
                        else
                        {
                            DateTime dt = Convert.ToDateTime(valueToParse);
                            valueToParse = dt.ToString("dd.MM.yyyy HH:mm");
                        }

                        break;
                }

                return valueToParse;
            }
            catch (Exception)
            {
                return valueToParse;
            }

        }

        private void UpdateValittuAlue(DataRow obj)
        {
            var objectid = obj[0];
            ValittuAlue = Alueet.FirstOrDefault(alue => alue.Attributes[ObjectIdField].ToString() == objectid.ToString());
        }

        private async void OnSulje()
        {
            Ok = false;
            await CloseViewModelAsync(true);
        }

        private async void OnOk()
        {
            Ok = true;
            await SaveViewModelAsync();
        }

        private async void OnZoomToGeometries()
        {
            if (ValittuAlue != null && CurrentLayer != null)
            {
                var ft = CurrentLayer.FeatureTable;
                if (ft != null)
                {
                    long objectId = Convert.ToInt64(ValittuAlue.Attributes[ft.ObjectIDField]);

                    if (objectId > 0)
                    {
                        var feature = await ft.QueryAsync(objectId);
                        if (feature != null)
                        {
                            CurrentLayer.SelectFeatures(new[] {objectId});
                            await MapUtils.Instance.ZoomToGeometryAsync(feature.Geometry);
                        }
                    }
                }
            }
        }
    }
}
