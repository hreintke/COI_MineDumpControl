using Mafi.Core.Input;
using Mafi.Core;
using Mafi.Unity.InputControl.Inspectors;
using Mafi.Unity.UserInterface;
using Mafi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mafi.Unity.InputControl.AreaTool;
using Mafi.Core.Terrain;
using Mafi.Core.Buildings.Mine;
using Mafi.Unity.Terrain;
using Mafi.Unity.InputControl;
using Mafi.Core.Buildings.Towers;
using Mafi.Unity.Mine;
using Mafi.Unity.Utils;

namespace MiningDumpingMod
{
    [GlobalDependency(RegistrationMode.AsEverything)]
    public class MDInspector : EntityInspector<MDTower, MDTowerWindowView>
    {
        private MDTowerWindowView _windowView;
        private readonly AreaSelectionTool _areaSelectionTool;
        private readonly TerrainRectSelection _terrainOutlineRenderer;
        private RectangleTerrainArea2i? _highlightedArea;
        private readonly ShortcutsManager _shortcutsManager;
        private readonly TowerAreasRenderer _towerAreasRenderer;
        private readonly IActivator _towerAreasAndDesignatorsActivator;

        public bool AreaEditInProgress;

        public MDInspector(InspectorContext inspectorContext, 
                                     AreaSelectionToolFactory areaToolFactory,
                                     TerrainRectSelection terrainOutlineRenderer,
                                     ShortcutsManager shortcutsManager,
                                     TowerAreasRenderer towerAreasRenderer) : base(inspectorContext)
        {
            _windowView = new MDTowerWindowView(this);
            _areaSelectionTool = areaToolFactory.CreateInstance((Action<RectangleTerrainArea2i, bool>)((x, y) => { }), new Action<RectangleTerrainArea2i, bool>(this.selectionDone), new Action(this.deactivateAreaEditing), (Action)(() => { }));
            _terrainOutlineRenderer = terrainOutlineRenderer;
            _shortcutsManager = shortcutsManager;
            _towerAreasRenderer = towerAreasRenderer;
            _towerAreasAndDesignatorsActivator = towerAreasRenderer.CreateCombinedActivatorWithTerrainDesignatorsAndGrid();
        }
  
        protected override MDTowerWindowView GetView() => this._windowView;

        private void selectionDone(RectangleTerrainArea2i area, bool leftClick)
        {
            SelectedEntity.editMinableArea(area);
            this.ToggleAreaEditing();
        }
        public void ToggleAreaEditing()
        {
            if (this.AreaEditInProgress)
            {
                this.deactivateAreaEditing();
                this._windowView.Show();
            }
            else
            {
                this._areaSelectionTool.TerrainCursor.Activate();
                this._windowView.Hide();
                this.AreaEditInProgress = true;
            }
        }

        private void deactivateAreaEditing()
        {
            if (!this.AreaEditInProgress)
                return;
            this._areaSelectionTool.Deactivate();
            this._areaSelectionTool.TerrainCursor.Deactivate();
            this._terrainOutlineRenderer.Hide();
            this._highlightedArea = new RectangleTerrainArea2i?();
            this.AreaEditInProgress = false;
        }

        public override bool InputUpdate(IInputScheduler inputScheduler)
        {
            if (this._areaSelectionTool.IsActive)
                return true;
            if (this.AreaEditInProgress)
            {
                if (this._shortcutsManager.IsPrimaryActionDown)
                {
                    this._terrainOutlineRenderer.Hide();
                    this._highlightedArea = new RectangleTerrainArea2i?();
                    this._areaSelectionTool.SetEdgeSizeLimit(new RelTile1i(this.SelectedEntity.Prototype.maxAreaSize)); //new RelTile1i(150));//
                    this._areaSelectionTool.Activate(true);
                    return true;
                }
                if (this._areaSelectionTool.TerrainCursor.HasValue)
                {
                    RectangleTerrainArea2i area = new RectangleTerrainArea2i(this._areaSelectionTool.TerrainCursor.Tile2i, RelTile2i.One);
                    RectangleTerrainArea2i rectangleTerrainArea2i = area;
                    RectangleTerrainArea2i? highlightedArea = this._highlightedArea;
                    if ((highlightedArea.HasValue ? (rectangleTerrainArea2i != highlightedArea.GetValueOrDefault() ? 1 : 0) : 1) != 0)
                    {
                        this._terrainOutlineRenderer.SetArea(area, AreaSelectionTool.SELECT_COLOR);
                        this._highlightedArea = new RectangleTerrainArea2i?(area);
                    }
                }
            }
            return base.InputUpdate(inputScheduler);
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            _towerAreasRenderer.SelectTowerArea(Option<IAreaManagingTower>.Some(SelectedEntity));
            _towerAreasAndDesignatorsActivator.Activate();
        }

        protected override void OnDeactivated()
        {
            base.OnDeactivated();
             _towerAreasAndDesignatorsActivator.Deactivate();
             _towerAreasRenderer.SelectTowerArea((Option<IAreaManagingTower>)Option.None);
            this.deactivateAreaEditing();
        }
    }

}
