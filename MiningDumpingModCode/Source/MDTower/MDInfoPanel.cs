using Mafi;
using Mafi.Collections;
using Mafi.Collections.ReadonlyCollections;
using Mafi.Core;
using Mafi.Core.Entities;
using Mafi.Core.Syncers;
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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;

namespace MiningDumpingMod;

public class MDInfoPanel : PanelWithHeader
{
    //private MDTower mdTower;

    BufferWithMultipleProductsUi productBuffer = new BufferWithMultipleProductsUi();

    PanelFooterRow panelFooterRow = new PanelFooterRow();

    Display desigAvailCount = new Display().MinDigits(4);
    Display thisMonth = new Display().MinDigits(4);
    Display lastMonth = new Display().MinDigits(4);

    public MDInfoPanel(
        MDTower mdTower,
        string panelHeaderTxt,
        string mdLabel,
        Func<Fix32> thisMonthQuantity,
        Func<Fix32> lastMonthQuantity,
        Func<IIndexable<ProductQuantity>> bufferProductQuantity, 
        Func<Quantity> maxBuffer,
        Action<bool> enableAction,
        Func<bool> checkStatus)
    {

        PanelFooterRow panelFooterRow = new PanelFooterRow();

        Display desigAvailCount = new Display().MinDigits(4);
        Display thisMonth = new Display().MinDigits(4);
        Display lastMonth = new Display().MinDigits(4);

        desigAvailCount.Value("0".AsLoc());
        thisMonth.Value("0".AsLoc()).ObserveValue((Func<LocStrFormatted>)(() => thisMonthQuantity().IntegerPart.ToString().AsLoc()));
        lastMonth.Value("0".AsLoc()).ObserveValue((Func<LocStrFormatted>)(() => lastMonthQuantity().IntegerPart.ToString().AsLoc()));

        Label desigLabel = new Label("Available Designations : ".AsLoc()).MarginLeftRight(2.px());
        Label mineLabel = new Label((mdLabel + " this/last month : ").AsLoc()).MarginLeftRight(2.px());
        Label slashLabel = new Label("/".AsLoc()).MarginLeftRight(1.px());

        Toggle enable =
             new Toggle(true).Label<Toggle>("Enabled".AsLoc())
                             .Tooltip<Toggle>("Only one of Mining or Dumping is allowed".AsLoc())
                             .OnValueChanged(enableAction)
                             .ObserveValue<Toggle>(checkStatus);
        
        Row desigInfo = new Row(1.px());
        desigInfo.Add(desigLabel, desigAvailCount);

        Row mineInfo = new Row(1.px());
        mineInfo.Add(mineLabel, thisMonth, slashLabel, lastMonth);
        mineInfo.Size<Row>(300.px());

        panelFooterRow.BodyAdd(desigInfo.AbsolutePosition(new Px?(), new Px?(), new Px?(), new Px?()).TextAlign(TextAlignment.CenterMiddle));
        panelFooterRow.BodyAdd(mineInfo.AbsolutePosition(new Px?(), new Px?(), new Px?(), 325.px()).TextCenterMiddle());
        panelFooterRow.Body.Height(31.px());

        this.Title(panelHeaderTxt.AsLoc()).BodyAdd(productBuffer, panelFooterRow);
        this.TitleRow.Add(enable.AbsolutePosition(new Px?(), new Px?(), new Px?(), 245));
        this.Collapsed(false);
       // this.Add(enable);

        LogWrite.Info($"Tilte children elements");


        this.ObserveIndexable<ProductQuantity>(bufferProductQuantity)
            .Observe<Quantity>(maxBuffer).Do((Action<Lyst<ProductQuantity>, Quantity>)((cargo, capacity) =>
            {
                productBuffer.SetProducts(cargo, capacity);
            }));
        
#if false
        PanelFooterRow pfro = new PanelFooterRow()
            .BodyAdd((Action<Row>)(c => c.JustifyItemsEnd<Row>()),
//            lo,
                    (UiComponent)new Toggle(true).Label<Toggle>("Dumping enabled".AsLoc())
                                .Tooltip<Toggle>("Only one of Mining or Dumping is allowed".AsLoc())
                                .OnValueChanged((Action<bool>)(isOn =>
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
                                }))
                                .ObserveValue<Toggle>((Func<bool>)(() => this.Entity.isDumping)));





        desigAvailCount.Value("10".AsLoc());
        thisMonth.Value("10".AsLoc()).ObserveValue((Func<LocStrFormatted>)(() => mdTower.thisMonthMined.IntegerPart.ToString().AsLoc()));
        lastMonth.Value("200".AsLoc()).ObserveValue((Func<LocStrFormatted>)(() => mdTower.lastMonthMined.IntegerPart.ToString().AsLoc()));

        Label desigLabel = new Label("Available Designations : ".AsLoc()).MarginLeftRight(2.px());
        Label mineLabel = new Label("Mined this/last month : ".AsLoc()).MarginLeftRight(2.px());
        Label slashLabel = new Label("/".AsLoc()).MarginLeftRight(1.px());

        Toggle enable =
             new Toggle(true).Label<Toggle>("Enabled".AsLoc())
                             .Tooltip<Toggle>("Only one of Mining or Dumping is allowed".AsLoc())
                             .OnValueChanged((Action<bool>)(isOn =>
                             {
                                 if (isOn)
                                 {
                                     mdTower.isMining = true;
                                     mdTower.isDumping = false;
                                 }
                                 else
                                 {
                                     mdTower.isMining = false;
                                 }
                             }))
                             .ObserveValue<Toggle>((Func<bool>)(() => mdTower.isMining));
        Row desigInfo = new Row(1.px());
        desigInfo.Add(desigLabel, desigAvailCount);

        Row mineInfo = new Row(1.px());
        mineInfo.Add(mineLabel, thisMonth, slashLabel, lastMonth);

        panelFooterRow.BodyAdd(desigInfo.AbsolutePosition(7.px(), new Px?(), new Px?(), new Px?()));
        panelFooterRow.BodyAdd(mineInfo.AbsolutePosition(7.px(), new Px?(), new Px?(), 352.px()));
        panelFooterRow.Body.Height(31.px());

        PanelWithHeader minePanel = new PanelWithHeader().Title("Mining Status".AsLoc()).BodyAdd(productBuffer, panelFooterRow);




        
        minePanel.TitleRow.Add(enable.AbsolutePosition(new Px?(), new Px?(), new Px?(), 245));
        minePanel.Collapsed(false);

        this.Body.Add(minePanel);



        this.ObserveIndexable<ProductQuantity>((Func<IIndexable<ProductQuantity>>)(() => mdTower.getMixedProductList(true)))
            .Observe<Quantity>((Func<Quantity>)(() => mdTower.mineBufferMax)).Do((Action<Lyst<ProductQuantity>, Quantity>)((cargo, capacity) =>
            {
                productBuffer.SetProducts(cargo, capacity);
            }));
#endif
    }

}
