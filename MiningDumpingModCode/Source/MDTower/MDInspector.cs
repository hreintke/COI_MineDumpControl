using Mafi;
using Mafi.Base;
using Mafi.Collections;
using Mafi.Collections.ReadonlyCollections;
using Mafi.Core;
using Mafi.Core.Buildings.Mine;
using Mafi.Core.Buildings.OreSorting;
using Mafi.Core.Buildings.Towers;
using Mafi.Core.Entities;
using Mafi.Core.Factory.Machines;
using Mafi.Core.Factory.Recipes;
using Mafi.Core.Products;
using Mafi.Core.Syncers;
using Mafi.Core.Terrain;
using Mafi.Localization;
using Mafi.Unity;
using Mafi.Unity.InputControl;
using Mafi.Unity.InputControl.AreaTool;
using Mafi.Unity.InputControl.GameMenu.Settings;
using Mafi.Unity.Mine;
using Mafi.Unity.Ui;
using Mafi.Unity.Ui.Controllers;
using Mafi.Unity.Ui.Library;
using Mafi.Unity.Ui.Library.Inspectors;
using Mafi.Unity.UiStatic;
using Mafi.Unity.UiToolkit.Component;
using Mafi.Unity.UiToolkit.Library;
using Mafi.Unity.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using AutoTerrainDesignations;

namespace MiningDumpingMod;

public class MDInspector : BaseInspector<MDTower>
{
    private readonly TowerAreasRenderer m_towerAreasRenderer;
    private readonly IActivator m_towerAreasAndDesignatorsActivator;
    private readonly PolygonAreaSelectionController m_areaSelectionTool;
    public bool AreaEditInProgress;
    private Option<MDTower> m_entityUnderEdit;

    public MDInspector(
      UiContext context,
      TowerAreasRenderer towerAreasRenderer,
      AssignedBuildingsHighlighter highlighter,
      BuildingsAssigner buildingsAssigner,
      NewInstanceOf<PolygonAreaSelectionController> areaSelectionTool) : base(context)
    {
        this.m_towerAreasRenderer = towerAreasRenderer;
        this.m_towerAreasAndDesignatorsActivator = towerAreasRenderer.CreateCombinedActivatorWithTerrainDesignatorsAndGrid();
        this.m_areaSelectionTool = areaSelectionTool.Instance;

        Row selectRow = new Row(1.pt());
        selectRow.Add(
            (UiComponent)new ButtonIconText(Button.Primary, "Assets/Unity/UserInterface/Toolbox/SelectArea.svg", (LocStrFormatted)Tr.ManagedArea__EditAction)
            .OnClick<ButtonIconText>(new Action(this.activateAreaEditing)), (UiComponent)new ButtonIcon(Button.General, "Assets/Unity/UserInterface/General/Search.svg")
            .OnClick<ButtonIcon>((Action)(() => context.CameraController.PanTo(Entity.Area.BoundingBoxCenter.CenterTile2f)))
            .Tooltip<ButtonIcon>(new LocStrFormatted?((LocStrFormatted)Tr.FocusManagedAreaTooltip)));
        StatusRow.Add(selectRow.AbsolutePosition(new Px?()));


        this.Observe<MDTower.State>((Func<MDTower.State>)(() => this.Entity.CurrentState)).Do((Action<MDTower.State>)(state =>
        {
            switch (state)
            {
                default:
                    this.Status.As(Tr.EntityStatus__Idle, DisplayState.Neutral);
                    break;
                case MDTower.State.None:
                    this.Status.As(Tr.EntityStatus__Idle, DisplayState.Neutral);
                    break;
                case MDTower.State.Working:
                    this.Status.AsWorking();
                    break;
                case MDTower.State.Paused:
                    this.Status.AsPaused();
                    break;
                case MDTower.State.NotEnoughWorkers:
                    this.Status.AsNoWorkers();
                    break;
            }
        }));

        Action<bool> mineEnable = (Action<bool>)(isOn =>
        {
            if (isOn)
            {
                Entity.isDumping = true;
                Entity.isMining = false;
            }
            else
            {
                Entity.isDumping = false;
            }
        });

        
#if AutoTerrainDesignations_enabled
        if (CustomEntityMod.ATD_Available)
        {
            PanelWithHeader bdp = ATDBridge.BuildDesignationPanel(() => { return this.Entity; }, this);
            bdp.Collapsed(true);
            this.Body.Add(bdp);
            PanelWithHeader bop = ATDBridge.BuildOreCompositionPanel(() => { return this.Entity; }, this);
            bop.Collapsed(true);
            this.Body.Add(bop);
        }
#endif
        MDInfoPanel mdp = new MDInfoPanel(Entity,
            "Mining Information",
            "  Mined",
            (Func<Fix32>)(() => Entity.thisMonthMined),
            (Func<Fix32>)(() => Entity.lastMonthMined),
            (Func<int>)(() => Entity.getDesignationCount(true)),
            (Func<IIndexable<ProductQuantity>>)(() =>
              {
                  return this.Entity.getMixedProductList(true);
              })
              ,
            (Func<Quantity>)(() => this.Entity.dumpBufferMax),
            (Action<bool>)(isOn =>
            {
                if (isOn)
                {
                    Entity.isDumping = false;
                    Entity.isMining = true;
                }
                else
                {
                    Entity.isMining = false;
                }
            }),
            (Func<bool>)(() => Entity.isMining)
            );

        this.Body.Add(mdp);


        MDInfoPanel ddp = new MDInfoPanel(Entity,
            "Dumping Information",
            "Dumped",
            (Func<Fix32>)(() => Entity.thisMonthDumped),
            (Func<Fix32>)(() => Entity.lastMonthDumped),
            (Func<int>)(() => Entity.getDesignationCount(false)),
            (Func<IIndexable<ProductQuantity>>)(() =>
            {
                return this.Entity.getMixedProductList(false);
            }),
            (Func<Quantity>)(() => this.Entity.dumpBufferMax),
            (Action<bool>)(isOn =>
            {
                if (isOn)
                {
                    Entity.isDumping = true;
                    Entity.isMining = false;
                }
                else
                {
                    Entity.isDumping = false;
                }
            }),
            (Func<bool>)(() => Entity.isDumping)
            );
        
        this.Body.Add(ddp);
    }

    protected override void OnActivated()
    {   
        base.OnActivated();
        this.m_towerAreasRenderer.HighlightTowerArea((Option<IAreaManagingTower>)this.Entity);
        this.m_towerAreasAndDesignatorsActivator.ActivateIfNotActive();
        this.m_entityUnderEdit = Option<MDTower>.None;
    }

    protected override void OnDeactivated()
    {
        base.OnDeactivated();
        if (this.m_entityUnderEdit.IsNone)
            this.m_towerAreasAndDesignatorsActivator.DeactivateIfActive();
        this.m_towerAreasRenderer.HighlightTowerArea((Option<IAreaManagingTower>)Option.None);
    }

    private void onAreaChanged(PolygonTerrainArea2i newArea)
    {
        if (!this.m_entityUnderEdit.HasValue)
            return;
        this.ScheduleCommand<MDAreaChangedCmd>(new MDAreaChangedCmd(this.m_entityUnderEdit.Value.Id, newArea));
    }

    private void deactivateEditing()
    {
        this.m_towerAreasAndDesignatorsActivator.DeactivateIfActive();
        this.m_towerAreasRenderer.MarkAreaUnderEdit(Option<IAreaManagingTower>.None);
    }

    private void reopen()
    {
        if (this.m_entityUnderEdit.HasValue)
            this.Context.InspectorsManager.TryActivateFor((IEntity)this.m_entityUnderEdit.Value);
        this.m_entityUnderEdit = Option<MDTower>.None;
    }

    private void activateAreaEditing()
    {
        this.m_entityUnderEdit = (Option<MDTower>)this.Entity;
        this.m_towerAreasRenderer.MarkAreaUnderEdit((Option<IAreaManagingTower>)this.Entity);
        this.m_areaSelectionTool.BeginEdit(this.Entity.Area, 400.ToFix32(), new Action(this.deactivateEditing), new Action(this.reopen), new Action<PolygonTerrainArea2i>(this.onAreaChanged));
    }
}

