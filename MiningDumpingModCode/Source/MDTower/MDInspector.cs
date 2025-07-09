using Mafi.Core.Buildings.Towers;
using Mafi.Core.Terrain;
using Mafi;
using Mafi.Unity.Ui;
using Mafi.Unity.Ui.Library.Inspectors;
using Mafi.Unity.UiToolkit.Component;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mafi.Unity.InputControl.AreaTool;
using Mafi.Unity.InputControl;
using Mafi.Unity.Mine;
using Mafi.Unity.Utils;
using Mafi.Base;
using Mafi.Core.Buildings.Mine;
using Mafi.Unity.UiToolkit.Library;
using Mafi.Localization;
using Mafi.Core.Syncers;
using Mafi.Core;
using Mafi.Unity.Ui.Library;
using Mafi.Unity.Ui.Controllers;
using Mafi.Core.Entities;

namespace MiningDumpingMod;

public class MDInspector : BaseInspector<MDTower>
{
    private readonly TowerAreasRenderer m_towerAreasRenderer;
    private readonly IActivator m_towerAreasAndDesignatorsActivator;
    private readonly PolygonAreaSelectionController m_areaSelectionTool;
    public bool AreaEditInProgress;
    private Option<MDTower> m_entityUnderEdit;

    ButtonText ea = new ButtonText("editarea".AsLoc());

    Row buttonRow = new Row().Gap(5).Margin(20);
    ButtonText mineButton = new ButtonText("Mining".AsLoc()).Width(100.px());
    ButtonText dumpButton = new ButtonText("Dumping".AsLoc()).Width(100.px());
    ButtonText stopButton = new ButtonText("Stop".AsLoc()).Width(100.px());
    ButtonText editButton = new ButtonText("Edit Area".AsLoc()).Width(100.px());

    ProductBufferUi miningBufferUi = new ProductBufferUi().Margin(20).Height(25);
    ProductBufferUi dumpingBuffferUi = new ProductBufferUi().Margin(20).Height(25);

    Label miningBufferLabel = new Label("MiningBuffer".AsLoc()).FontSize(15).Margin(20);
    Label dumpingBufferLabel = new Label("DumpinfBuffer".AsLoc()).FontSize(15).Margin(20);

    Panel buttonPanel = new Panel();


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

        mineButton.OnClick(() => { Entity.setMining(true); });
        dumpButton.OnClick(() => { Entity.setDumping(true); });
        stopButton.OnClick(() => { Entity.setMining(false); Entity.setDumping(false); });
        editButton.OnClick(activateAreaEditing);

        buttonRow.Add(mineButton);
        buttonRow.Add(dumpButton);
        buttonRow.Add(stopButton);
        buttonRow.Add(editButton);

        buttonPanel.Add(buttonRow);
        buttonPanel.Add(miningBufferLabel);
        buttonPanel.Add(miningBufferUi);
        buttonPanel.Add(dumpingBufferLabel);
        buttonPanel.Add(dumpingBuffferUi);

        this.Body.Add(buttonPanel);


        this.Observe<MDTower.State>((Func<MDTower.State>)(() => this.Entity.CurrentState)).Do((Action<MDTower.State>)(state =>
        {
            switch (state)
            {
                case MDTower.State.None:
                    this.Status.As(Tr.EntityStatus__Idle, DisplayState.Neutral);
                    break;
                case MDTower.State.Working:
                    if (Entity.isMining)
                    {
                        this.Status.As("Mining".AsLoc(), DisplayState.Positive);
                    }
                    else if (Entity.isDumping)
                    {
                        this.Status.As("Dumping".AsLoc(), DisplayState.Positive);
                    }
                    else
                    {
                        this.Status.AsWorking();
                    }
                    break;
                case MDTower.State.Paused:
                    this.Status.AsPaused();
                    break;
                case MDTower.State.NotEnoughWorkers:
                    this.Status.AsNoWorkers();
                    break;
                case MDTower.State.Waiting:
                    this.Status.As("Waiting".AsLoc(), DisplayState.Positive);
                    break;
                case MDTower.State.BufferIssue:
                    if (Entity.isMining)
                    {
                        this.Status.As("Mining Buffer Full".AsLoc(), DisplayState.Positive);
                    }
                    else if (Entity.isDumping)
                    {
                        this.Status.As("Dumping Buffer Empty".AsLoc(), DisplayState.Positive);
                    }
                    else
                    {
                        this.Status.As("Buffer Issue".AsLoc(), DisplayState.Positive);
                    }
                    break;
            }
        }));

        this.Observe<Quantity>((Func<Quantity>)(() => this.Entity.mineBufferQuantity)).Do(q => { miningBufferUi.Values(Entity.mineBufferQuantity, Entity.mineBufferMax); });
        this.Observe<Quantity>((Func<Quantity>)(() => this.Entity.dumpBufferQuantity)).Do(q => { dumpingBuffferUi.Values(Entity.dumpBufferQuantity, Entity.dumpBufferMax); });
    }

    protected override void OnActivated()
    {   
        base.OnActivated();
        LogWrite.Info($"activating MD inspector {this.Entity.Id}");
        this.m_towerAreasRenderer.HighlightTowerArea((Option<IAreaManagingTower>)this.Entity);
        this.m_towerAreasAndDesignatorsActivator.ActivateIfNotActive();
        this.m_entityUnderEdit = Option<MDTower>.None;
        LogWrite.Info($"activated  MD inspector {this.Entity.Id}");
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
        this.ScheduleCommand<MineTowerAreaChangeCmd>(new MineTowerAreaChangeCmd(this.m_entityUnderEdit.Value.Id, newArea));
        m_entityUnderEdit.Value.editMinableArea(newArea);
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

