using Mafi.Unity.UiFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mafi.Collections;
using Mafi.Collections.ReadonlyCollections;
using Mafi.Core;
using Mafi.Core.Entities;
using Mafi.Core.Entities.Static;
using Mafi.Core.Input;
using Mafi.Core.Syncers;
using Mafi.Unity.UserInterface;
using Mafi.Unity.InputControl.Inspectors;
using Mafi.Unity.InputControl;
using Mafi;
using Mafi.Unity;

// This code is from a decompiled version of Mafi.Unity.InputControl.Inspectors.EntityInspector`2
// Included here because the original in an internal class

namespace MiningDumpingMod
{
    public abstract class EntityInspector<TEntity, TView> :
       IUnityUi,
       IEntityInspector<TEntity>,
       IEntityInspector,
       IEntityInspectorFactory<TEntity>,
       IFactory<TEntity, IEntityInspector>
       where TEntity : class, IEntity, IRenderedEntity
       where TView : ItemDetailWindowView
    {
        private Option<BuildingsAssigner> m_buildingsAssigner;
        private bool m_restoreHighlightAfterUpgrade;
        private Option<IStaticEntityWithReservedOcean> m_activatedOverlayEntity;
        private readonly Lyst<IRenderedEntity> m_secondaryHighlightedEntities;
        protected TView WindowView;
        private TEntity m_selectedEntity;

        public IInputScheduler InputScheduler => this.Context.InputScheduler;

        protected IUnityInputMgr InputMgr => this.Context.InputMgr;

        protected InspectorController Controller => this.Context.MainController;

        /// <summary>
        /// Entity is null when this inspector is deactivated and not null when activated. We do not use option here
        /// because any time this class is used it has to be activated first.
        /// </summary>
        public TEntity SelectedEntity => this.m_selectedEntity;

        public InspectorContext Context { get; }

        public virtual bool DeactivateOnNonUiClick => true;

        protected EntityInspector(InspectorContext context)
        {
            this.m_secondaryHighlightedEntities = new Lyst<IRenderedEntity>();
            // ISSUE: explicit constructor call
            this.Context = context;
        }

        public virtual void RegisterUi(UiBuilder builder)
        {
            this.WindowView = this.GetView();
            this.WindowView.SetOnCloseButtonClickAction((Action)(() => this.InputMgr.DeactivateController((IUnityInputController)this.Controller)));
            this.WindowView.BuildUi(builder);
            if (!this.m_buildingsAssigner.HasValue)
                return;
            this.WindowView.AddUpdater(this.m_buildingsAssigner.Value.Updater);
        }

        public IEntityInspector Create(TEntity entity)
        {
            this.m_selectedEntity = entity.CheckNotNull<TEntity>();
            return (IEntityInspector)this;
        }

        public void Activate()
        {
            Assert.That<TEntity>(this.SelectedEntity).IsNotNull<TEntity>();
            if (this.m_buildingsAssigner.HasValue)
                this.m_buildingsAssigner.Value.SetEntity((IEntity)this.SelectedEntity);
            this.Context.Highlighter.Highlight((IRenderedEntity)this.SelectedEntity, ColorRgba.Yellow);
            this.OnActivated();
            this.WindowView.Show();
        }

        public void Deactivate()
        {
            if (this.m_buildingsAssigner.HasValue)
            {
                this.m_buildingsAssigner.Value.DeactivateTool();
                this.Controller.SetHoverCursorSuppression(false);
            }
            this.WindowView.Hide();
            this.OnDeactivated();
            this.Context.Highlighter.RemoveHighlight((IRenderedEntity)this.SelectedEntity);
            this.RemoveSecondaryHighlight();
            Assert.That<int>(this.Context.Highlighter.HighlightedCount).IsZero();
            this.m_selectedEntity = default(TEntity);
        }

        protected void ForceDeactivate()
        {
            this.InputMgr.DeactivateController((IUnityInputController)this.Controller);
        }

        protected void HighlightSecondaryEntity(IRenderedEntity entity)
        {
            this.RemoveSecondaryHighlight();
            this.m_secondaryHighlightedEntities.Add(entity);
            this.m_secondaryHighlightedEntities.ForEach((Action<IRenderedEntity>)(x => this.Context.Highlighter.Highlight(x, ColorRgba.Blue)));
        }

        protected void HighlightSecondaryEntities<T>(IEnumerable<T> entities) where T : IRenderedEntity
        {
            this.RemoveSecondaryHighlight();
            foreach (T entity in entities)
                this.m_secondaryHighlightedEntities.Add((IRenderedEntity)entity);
            this.m_secondaryHighlightedEntities.ForEach((Action<IRenderedEntity>)(x => this.Context.Highlighter.Highlight(x, ColorRgba.Blue)));
        }

        protected void RemoveSecondaryHighlight()
        {
            this.m_secondaryHighlightedEntities.ForEach((Action<IRenderedEntity>)(x => this.Context.Highlighter.RemoveHighlight(x)));
            this.m_secondaryHighlightedEntities.Clear();
        }

        public virtual void SyncUpdate(GameTime gameTime)
        {
            Assert.That<TEntity>(this.SelectedEntity).IsNotNull<TEntity>();
            if (this.SelectedEntity.IsDestroyed)
                this.ForceDeactivate();
            else
                this.WindowView.SyncUpdate(gameTime);
        }

        public virtual void RenderUpdate(GameTime gameTime)
        {
            if (this.m_buildingsAssigner.HasValue)
                this.m_buildingsAssigner.Value.RenderUpdate();
            if (this.m_restoreHighlightAfterUpgrade)
            {
                this.Context.Highlighter.Highlight((IRenderedEntity)this.SelectedEntity, ColorRgba.Yellow);
                this.m_restoreHighlightAfterUpgrade = false;
            }
            this.WindowView.RenderUpdate(gameTime);
        }

        public virtual bool InputUpdate(IInputScheduler inputScheduler)
        {
            return !this.m_buildingsAssigner.IsNone && this.m_buildingsAssigner.Value.InputUpdate(inputScheduler);
        }

        public T ScheduleInputCmd<T>(T cmd) where T : IInputCommand
        {
            return this.InputScheduler.ScheduleInputCmd<T>(cmd);
        }

        public void Clear()
        {
            if (!((object)this.SelectedEntity is StaticEntity))
                return;
            this.WindowView.RenderUpdate(new GameTime());
        }

        /// <summary>
        /// If you have a buildings assigner set it here and its lifecycle and control will be managed for you.
        /// </summary>
        protected void SetBuildingsAssigner(BuildingsAssigner buildingsAssigner)
        {
            this.m_buildingsAssigner = (Option<BuildingsAssigner>)buildingsAssigner;
        }

        public void EditInputBuildingsClicked()
        {
            Assert.That<Option<BuildingsAssigner>>(this.m_buildingsAssigner).HasValue<BuildingsAssigner>();
            if (this.m_buildingsAssigner.IsNone)
                return;
            this.Controller.SetHoverCursorSuppression(true);
            this.m_buildingsAssigner.Value.ActivateTool((Action)(() => this.InputMgr.DeactivateController((IUnityInputController)this.Controller)), true);
            this.WindowView.Hide();
        }

        public void EditOutputBuildingsClicked()
        {
            Assert.That<Option<BuildingsAssigner>>(this.m_buildingsAssigner).HasValue<BuildingsAssigner>();
            if (this.m_buildingsAssigner.IsNone)
                return;
            this.Controller.SetHoverCursorSuppression(true);
            this.m_buildingsAssigner.Value.ActivateTool((Action)(() => this.InputMgr.DeactivateController((IUnityInputController)this.Controller)), false);
            this.WindowView.Hide();
        }

        protected IUiUpdater CreateVehiclesUpdater()
        {
            Assert.That<Type>(typeof(TEntity)).IsAssignableTo<IEntityAssignedWithVehicles>();
            return UpdaterBuilder.Start().Observe<Mafi.Core.Entities.Dynamic.Vehicle>((Func<IIndexable<Mafi.Core.Entities.Dynamic.Vehicle>>)(() => ((IEntityAssignedWithVehicles)(object)this.SelectedEntity).AllSpawnedVehicles()), (ICollectionComparator<Mafi.Core.Entities.Dynamic.Vehicle, IIndexable<Mafi.Core.Entities.Dynamic.Vehicle>>)CompareFixedOrder<Mafi.Core.Entities.Dynamic.Vehicle>.Instance).Do(new Action<Lyst<Mafi.Core.Entities.Dynamic.Vehicle>>(this.HighlightSecondaryEntities<Mafi.Core.Entities.Dynamic.Vehicle>)).Build(SyncFrequency.MoreThanSec);
        }

        /// <summary>Called only once. Safe to create the window there.</summary>
        protected abstract TView GetView();

        protected virtual void OnActivated()
        {
            if (!(this.SelectedEntity is IStaticEntityWithReservedOcean selectedEntity))
                return;
            this.m_activatedOverlayEntity = this.Context.OceanOverlayRenderer.ActivateForSingleEntity(selectedEntity);
        }

        protected virtual void OnDeactivated()
        {
            if (!this.m_activatedOverlayEntity.HasValue)
                return;
            this.m_activatedOverlayEntity = this.Context.OceanOverlayRenderer.DeactivateForSingleEntity(this.m_activatedOverlayEntity);
        }
    }
}

