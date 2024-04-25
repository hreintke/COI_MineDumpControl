using Mafi.Unity.InputControl.Inspectors;
using Mafi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mafi.Unity.UiFramework.Components;
using Mafi.Unity.UserInterface;
using Mafi.Core;
using Mafi.Core.Input;
using System.Runtime.Remoting.Lifetime;
using Mafi.Unity.UiFramework;
using Mafi.Unity.InputControl;
using Mafi.Core.Products;
using Mafi.Core.Syncers;
using Mafi.Base.Prototypes.Machines.ComputingEntities;
using Mafi.Unity;
using Mafi.Unity.UserInterface.Components;
using UnityEngine;
using Mafi.Logging;

namespace MiningDumpingMod
{
    public class MDTowerWindowView : StaticEntityInspectorBase<MDTower>
    {
        private readonly MDInspector _inspector;

        private Btn CustomEntityButton;
        private Txt sliderLabel;
        private Txt simStepLabel;
        private Txt debugLabel;
        private Txt progressLabel;
        private BufferView mineBufferView;
        private QuantityBar mineBufferBar;
        private QuantityBar dumpBufferBar;

        private ProtosFilterEditor<ProductProto> m_filterView;
        private TxtField txtField;

        public MDTowerWindowView(MDInspector inspector) : base(inspector)
        {
            _inspector = inspector;
        }

        protected override MDTower Entity => _inspector.SelectedEntity;

        Btn AddMDButton(StackContainer parent, Action action, string title)
        {
            return Builder.NewBtnGeneral(title, parent)
                .SetButtonStyle(Style.Global.GeneralBtn).SetText(title)
                .OnClick(action)
                .AppendTo(parent, new Vector2(80, 25), ContainerPosition.MiddleOrCenter, Offset.Top(5f));
        }

        StackContainer AddInputValue(StackContainer parent, Action<string> action, string title)
        {
            StackContainer inputValue = Builder.NewStackContainer("xvalues")
                .SetStackingDirection(StackContainer.Direction.LeftToRight)
                .SetItemSpacing(5f)
                .SetHeight(30);

            Builder.NewTxt("XL")
                .SetHeight(25)
                .SetTextStyle(Builder.Style.Global.TextControls)
                .SetText(title)
                .AppendTo(inputValue, new Vector2(30, 25), ContainerPosition.MiddleOrCenter, new Offset(0, 5, 20, 0));

            Builder.NewTxtField("X")
                .SetHeight(25f)
                .SetDelayedOnEditEndListener(action)
                .AppendTo(inputValue, new Vector2(80, 25), ContainerPosition.MiddleOrCenter, Offset.Top(5f));

            return inputValue.AppendTo(parent);
        }

        protected override void AddCustomItems(StackContainer itemContainer)
        {
            string et = (Entity == null) ? "Null" : Entity.Id.ToString(); 
            UpdaterBuilder updaterBuilder = UpdaterBuilder.Start();
            base.AddCustomItems(itemContainer);
            
            AddSectionTitle(itemContainer, "Control actions");

            StackContainer buttonStack = Builder.NewStackContainer("buttons")
                .SetStackingDirection(StackContainer.Direction.LeftToRight)
                .SetItemSpacing(5f)
                .SetHeight(40);

            AddMDButton(buttonStack, () => { Entity.setMining(true); }, "Mining");
            AddMDButton(buttonStack, () => { Entity.setDumping(true); }, "Dumping");
            AddMDButton(buttonStack, () => { Entity.setMining(false);Entity.setDumping(false); }, "Stop");
            AddMDButton(buttonStack, () => { _inspector.ToggleAreaEditing();}, "Edit Area");

            buttonStack.AppendTo(ItemsContainer);

            AddSectionTitle(itemContainer, "Mining Buffer");
            mineBufferBar = new QuantityBar(Builder);
            mineBufferBar.SetHeight(30);
            mineBufferBar.AppendTo(ItemsContainer, null, new Offset(20, 0, 20, 0));

            AddSectionTitle(itemContainer, "Dump Buffer");
            dumpBufferBar = new QuantityBar(Builder);
            dumpBufferBar.SetHeight(30);
            dumpBufferBar.AppendTo(ItemsContainer, null, new Offset(20, 0, 20, 0));
#if false
            AddSectionTitle(itemContainer, "Speed Value");
            
            StackContainer speedStack = Builder.NewStackContainer("speed")
                .SetStackingDirection(StackContainer.Direction.LeftToRight)
                .SetItemSpacing(5f)
                .SetHeight(40);

            var simStepSlider = Builder
                .NewSlider("simStepSlider")
                .SimpleSlider(Builder.Style.Panel.Slider)
                .SetValuesRange(1f, 10f)
                .WholeNumbersOnly()
                .OnValueChange(
                    stepCount => {  },
                    stepCount =>
                    {
                        simStepLabel.SetText((((int)Math.Round(stepCount)).ToString()));
                        Entity.simStepChanged((int)Math.Round(stepCount));
                    })
                .SetValue(5f)
                .SetHeight(20);

            simStepSlider.AppendTo(speedStack, null, new Offset(20,0,20,0));

            simStepLabel = Builder
                .NewTxt("")
                .SetTextStyle(Builder.Style.Global.TextControls)
                .SetText("Default text");

            simStepLabel.AppendTo(speedStack, 30f, new Offset(0,5,0,0));

            speedStack.AppendTo(itemContainer);

            AddSectionTitle(itemContainer, "Progress");

            progressLabel = Builder
                .NewTxt("")
                .SetTextStyle(Builder.Style.Global.TextControls)
                .SetText("Default text");
            progressLabel.AppendTo(itemContainer);

            AddSectionTitle(itemContainer, "Info Text");

            sliderLabel = Builder
                .NewTxt("")
                .SetTextStyle(Builder.Style.Global.TextControls)
                .SetText("Default text");
            sliderLabel.AppendTo(itemContainer);

            AddSectionTitle(itemContainer, "Debug text");
            debugLabel = Builder
                .NewTxt("")
                .SetTextStyle(Builder.Style.Global.TextControls)
                .SetText("Default text");
            debugLabel.AppendTo(itemContainer);

#endif

            StatusPanel statusInfo = AddStatusInfoPanel();
            updaterBuilder.Observe<MDTower.State>((Func<MDTower.State>)(() => this.Entity.CurrentState)).Do((Action<MDTower.State>)(state =>
            {
                switch (state)
                {
                    case MDTower.State.None:
                        statusInfo.SetStatus(Tr.EntityStatus__Idle, StatusPanel.State.Warning);
                        break;
                    case MDTower.State.Working:
                        if (Entity.isMining)
                        {
                            statusInfo.SetStatus("Mining", StatusPanel.State.Ok);
                        }
                        else if (Entity.isDumping) 
                        {
                            statusInfo.SetStatus("Dumping", StatusPanel.State.Ok);
                        }
                        else
                        {
                            statusInfo.SetStatusWorking();
                        }
                        break;
                    case MDTower.State.Paused:
                        statusInfo.SetStatusPaused();
                        break;
                    case MDTower.State.NotEnoughWorkers:
                        statusInfo.SetStatusNoWorkers();
                        break;
                    case MDTower.State.Waiting:
                        statusInfo.SetStatus("Waiting", StatusPanel.State.Ok);
                        break;
                    case MDTower.State.BufferIssue:
                        if (Entity.isMining)
                        {
                            statusInfo.SetStatus("Mining Buffer Full", StatusPanel.State.Ok);
                        }
                        else if (Entity.isDumping)
                        {
                            statusInfo.SetStatus("Dumping Buffer Empty", StatusPanel.State.Ok);
                        }
                        else
                        {
                            statusInfo.SetStatus("Buffer Issue", StatusPanel.State.Ok);
                        }
                        break;
                }
            }));
            updaterBuilder.Observe<int>((Func<int>)(() => this.Entity.simStepCount)).Do((Action<int>)(pc => { simStepLabel.SetText(Entity.simStepCount.ToString()); }));
            updaterBuilder.Observe<Quantity>((Func<Quantity>)(() => this.Entity.mineBufferQuantity)).Do(q => { mineBufferBar.UpdateValues(Entity.mineBufferMax, Entity.mineBufferQuantity); });
            updaterBuilder.Observe<Quantity>((Func<Quantity>)(() => this.Entity.dumpBufferQuantity)).Do(q => { dumpBufferBar.UpdateValues(Entity.dumpBufferMax, Entity.dumpBufferQuantity); });

            this.AddUpdater(updaterBuilder.Build());
        }
    }
}
