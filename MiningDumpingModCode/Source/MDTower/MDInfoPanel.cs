using Mafi;
using Mafi.Collections;
using Mafi.Collections.ReadonlyCollections;
using Mafi.Core;
using Mafi.Core.Entities;
using Mafi.Core.Entities.Static;
using Mafi.Core.Syncers;
using Mafi.Localization;
using Mafi.Unity.Ui.Library;
using Mafi.Unity.Ui.Library.Inspectors;
using Mafi.Unity.UiToolkit.Component;
using Mafi.Unity.UiToolkit.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiningDumpingMod;

public class MDInfoPanel : PanelWithHeader
{
    Label toggleStateLabel = new Label("Toggle state : ".AsLoc()).MarginLeftRight(2.px());

    BufferWithMultipleProductsUi productBuffer = new BufferWithMultipleProductsUi();

    PanelFooterRow panelFooterRow = new PanelFooterRow().Gap(50.px()).Height(31.px());

    Display desigAvailCount = new Display().MinDigits(4);
    Display thisMonth = new Display().MinDigits(4);
    Display lastMonth = new Display().MinDigits(4);

    Label desigLabel = new Label("Available Designations : ".AsLoc()).MarginLeftRight(2.px());
    Label mineDumpLabel = new Label();
    Label slashLabel = new Label("/".AsLoc()).MarginLeftRight(1.px());

    Toggle enable = new Toggle();


    public MDInfoPanel(
        MDTower mdTower,
        string panelHeaderTxt,
        string mdLabel,
        Func<Fix32> thisMonthQuantity,
        Func<Fix32> lastMonthQuantity,
        Func<int> availableDesignators,
        Func<IIndexable<ProductQuantity>> bufferProductQuantity,
        Func<Quantity> maxBuffer,
        Action<bool> enableAction,
        Func<bool> checkStatus)
    {
        mineDumpLabel= new Label((mdLabel + " this/last month : ").AsLoc()).MarginLeftRight(2.px());
        enable = new Toggle(true).Label<Toggle>("Enabled".AsLoc())
                             .Tooltip<Toggle>("Only one of Mining or Dumping is allowed".AsLoc())
                             .OnValueChanged(enableAction)
                             .ObserveValue<Toggle>(checkStatus);

        desigAvailCount.Value("0".AsLoc());
        thisMonth.Value("0".AsLoc());//.ObserveValue((Func<LocStrFormatted>)(() => thisMonthQuantity().IntegerPart.ToString().AsLoc()));
        lastMonth.Value("0".AsLoc());//.ObserveValue((Func<LocStrFormatted>)(() => lastMonthQuantity().IntegerPart.ToString().AsLoc()));


        Row desigInfo = new Row(1.px());
        desigInfo.Add(desigLabel, desigAvailCount);

        Row mineInfo = new Row(1.px());
        mineInfo.Add(mineDumpLabel, thisMonth, slashLabel, lastMonth);

        UiComponent[] uiComponentArray2 = new UiComponent[2]
        {
            (UiComponent) desigInfo,
            (UiComponent) mineInfo
        };

        panelFooterRow.Add(desigInfo.AbsolutePosition(new Px(4), new Px?(), new Px?(), new Px?()).TextAlign(TextAlignment.CenterMiddle));
        panelFooterRow.Add(mineInfo.AbsolutePosition(new Px(4), new Px?(), new Px?(), new Px(320)).TextCenterMiddle());

        this.Body.MarginLeftRight(10.px());

        this.Title(panelHeaderTxt.AsLoc());
        this.BodyAdd(productBuffer);
        this.BodyAdd(panelFooterRow);

        this.TitleRow.Add(enable.AbsolutePosition(new Px?(), new Px?(), new Px?(), 245));

        this.ObserveIndexable<ProductQuantity>(bufferProductQuantity)
            .Observe<Quantity>(maxBuffer).Do((Action<Lyst<ProductQuantity>, Quantity>)((cargo, capacity) =>
            {
                productBuffer.SetProducts(cargo, capacity);
            }));

        this.Observe<Fix32>(thisMonthQuantity).Do((Action<Fix32>)((q) => thisMonth.Value(q.IntegerPart)));

        this.Observe<Fix32>(lastMonthQuantity).Do((Action<Fix32>)((q) => lastMonth.Value(q.IntegerPart)));

        this.Observe<int>(availableDesignators).Do((Action<int>)(((a) => desigAvailCount.Value(a))));
    }
}

    

                   
