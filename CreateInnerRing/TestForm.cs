using System;
using System.Windows.Forms;
using System.Collections.ObjectModel;
using ThinkGeo.MapSuite.Core;
using ThinkGeo.MapSuite.DesktopEdition;

//In this project, you track a polygon. You need to end it by double clicking. It will find all the counties completely within the tracked polygon and it will
//create a inner ring polygon based on the tracked polygon (outer ring) and the selected counties (inner ring).
namespace  CreateInnerRing
{
    public partial class TestForm : Form
    {
        public TestForm()
        {
            InitializeComponent();
        }

        private void TestForm_Load(object sender, EventArgs e)
        {
            winformsMap1.MapUnit = GeographyUnit.DecimalDegree;
            winformsMap1.CurrentExtent = new RectangleShape(-98.9883,33.6736,-95.4177,31.5258);
            winformsMap1.BackgroundOverlay.BackgroundBrush = new GeoSolidBrush(GeoColor.FromArgb(255, 198, 255, 255));

            //Displays the World Map Kit as a background.
            ThinkGeo.MapSuite.DesktopEdition.WorldMapKitWmsDesktopOverlay worldMapKitDesktopOverlay = new ThinkGeo.MapSuite.DesktopEdition.WorldMapKitWmsDesktopOverlay();
            winformsMap1.Overlays.Add(worldMapKitDesktopOverlay);

            //Shapefile of counties
            ShapeFileFeatureLayer shapeFileFeatureLayer = new ShapeFileFeatureLayer(@"../../data/Counties.shp");
            shapeFileFeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultAreaStyle = AreaStyles.CreateSimpleAreaStyle(GeoColor.StandardColors.Transparent, GeoColor.StandardColors.DarkGray, 2);
            shapeFileFeatureLayer.ZoomLevelSet.ZoomLevel01.ApplyUntilZoomLevel = ApplyUntilZoomLevel.Level20;

            LayerOverlay layerOverlay = new LayerOverlay();
            layerOverlay.Layers.Add("CountiesLayer",shapeFileFeatureLayer);
            winformsMap1.Overlays.Add("LayerOverlay", layerOverlay);

            //Mode Track Polygon and sets event when ending tracking polygon
            winformsMap1.TrackOverlay.TrackMode = TrackMode.Polygon;
            winformsMap1.TrackOverlay.TrackEnded += new EventHandler<TrackEndedTrackInteractiveOverlayEventArgs>(trackOverlay_TrackEnded);

            //InMemoryFeatureLayer for showing the result
            InMemoryFeatureLayer inMemoryFeatureLayer = new InMemoryFeatureLayer();
            inMemoryFeatureLayer.ZoomLevelSet.ZoomLevel01.DefaultAreaStyle = AreaStyles.CreateSimpleAreaStyle(GeoColor.FromArgb(150, GeoColor.StandardColors.Red));
            inMemoryFeatureLayer.ZoomLevelSet.ZoomLevel01.ApplyUntilZoomLevel = ApplyUntilZoomLevel.Level20;

            LayerOverlay dynamicOverlay = new LayerOverlay();
            dynamicOverlay.Layers.Add("DynamicLayer", inMemoryFeatureLayer);
            winformsMap1.Overlays.Add("DynamicOverlay", dynamicOverlay);

            winformsMap1.Refresh();
        }

        private void trackOverlay_TrackEnded(object sender ,TrackEndedTrackInteractiveOverlayEventArgs e)
        {
            //Gets the shape from the tracked polygon. The tracked shapes can be found in the TrackShapeLayer of the TrackOverlay.
            AreaBaseShape areaBaseShape = (AreaBaseShape)winformsMap1.TrackOverlay.TrackShapeLayer.InternalFeatures[0].GetShape();

            //Gets the features of the counties that are completely within the tracked polygon.
            LayerOverlay layerOverlay = (LayerOverlay)winformsMap1.Overlays["LayerOverlay"];
            ShapeFileFeatureLayer shapeFileFeatureLayer = (ShapeFileFeatureLayer)layerOverlay.Layers["CountiesLayer"];
            Collection<Feature> features = shapeFileFeatureLayer.QueryTools.GetFeaturesWithin(areaBaseShape, ReturningColumnsType.NoColumns);

            //Adds the shape of the selected counties features to AreaBaseShape collection to perform the Union.
            Collection<AreaBaseShape> areaBaseShapes = new Collection<AreaBaseShape>();
            foreach (Feature feature in features)
            {
                areaBaseShapes.Add((AreaBaseShape)feature.GetShape());
            }
            MultipolygonShape unionMultipolygonShape = MultipolygonShape.Union(areaBaseShapes);

            //Gets the MultiPolygonShape with the inner ring(s) being the result of the Union of the selected shapes.
            MultipolygonShape resultMultiPolygonShape = areaBaseShape.GetDifference(unionMultipolygonShape);

            //Adds the result to the InMemoryFeatureLayer for display on the map.
            LayerOverlay dynamicOverlay = (LayerOverlay)winformsMap1.Overlays["DynamicOverlay"];
            InMemoryFeatureLayer inMemoryFeatureLayer = (InMemoryFeatureLayer)dynamicOverlay.Layers["DynamicLayer"];
            inMemoryFeatureLayer.InternalFeatures.Clear();

            inMemoryFeatureLayer.InternalFeatures.Add(new Feature(resultMultiPolygonShape));

            //We clear the tracked shape so that it does not show on the map.
            winformsMap1.TrackOverlay.TrackShapeLayer.InternalFeatures.Clear();

            winformsMap1.Refresh(winformsMap1.Overlays["DynamicOverlay"]);
        }

        private void winformsMap1_MouseMove(object sender, MouseEventArgs e)
        {
            //Displays the X and Y in screen coordinates.
            statusStrip1.Items["toolStripStatusLabelScreen"].Text = "X:" + e.X + " Y:" + e.Y;

            //Gets the PointShape in world coordinates from screen coordinates.
            PointShape pointShape = ExtentHelper.ToWorldCoordinate(winformsMap1.CurrentExtent, new ScreenPointF(e.X, e.Y), winformsMap1.Width, winformsMap1.Height);

            //Displays world coordinates.
            statusStrip1.Items["toolStripStatusLabelWorld"].Text = "(world) X:" + Math.Round(pointShape.X, 4) + " Y:" + Math.Round(pointShape.Y, 4);
        }
        
        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

       

    }
}
