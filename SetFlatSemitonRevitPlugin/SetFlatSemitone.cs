using System;
using System.Linq;
using System.Text;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace SetFlatSemitoneRevitPlugin
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class SetFlatSemitone : IExternalCommand
    {
        private const string LevelParam = "Уровень";
        private const string SectorParam = "BS_Блок";
        private const string FlatTypeParam = "ROM_Подзона";
        private const string FlatParam = "ROM_Зона";

        /// <summary>Overload this method to implement and external command within Revit.</summary>
        /// <returns> The result indicates if the execution fails, succeeds, or was canceled by user. If it does not
        /// succeed, Revit will undo any changes made by the external command. </returns>
        /// <param name="commandData"> An ExternalCommandData object which contains reference to Application and View
        /// needed by external command.</param>
        /// <param name="message"> Error message can be returned by external command. This will be displayed only if the command status
        /// was "Failed".  There is a limit of 1023 characters for this message; strings longer than this will be truncated.</param>
        /// <param name="elements"> Element set indicating problem elements to display in the failure dialog.  This will be used
        /// only if the command status was "Failed".</param>
        public Result Execute(
            ExternalCommandData commandData, 
            ref string message, 
            ElementSet elements)
        {
            //Получение объектов приложения и документа
            var uiApp = commandData.Application;
            var doc = uiApp.ActiveUIDocument.Document;

            // Фильтруем элементы, чтобы получить только комнаты квартир
            var rooms = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WherePasses(new ElementParameterFilter(new FilterStringRule(new ParameterValueProvider(new ElementId(10616104)), new FilterStringContains(), "Квартира", false)))
                .ToElements();

            // Группируем комнаты по этажам, секциям, типу квартиры и самим квартирам
            var groupingFlats =
                rooms.GroupBy(GetRoomGroupingKey)
                    .ToDictionary(grouping => grouping.Key, grouping => grouping.GroupBy(GetFlatNumber)
                        .Select(element => new Flat(element.Key, element.Select(room => room).ToList()))
                        .ToList());

            var trans = new Transaction(doc, "SetFlatSemitone");
            trans.Start();
            try
            {
                foreach (var flats in groupingFlats)
                {
                    if (flats.Value.Count < 2)
                        continue;

                    // Подкрашиваем смежные квартиры (по сквозной нумерации) из одной группы в полутон
                    var sortedFlats = flats.Value.OrderBy(flat => flat.Number).ToList();
                    for (var i = 0; i < sortedFlats.Count - 1; i++)
                    {
                        if ((sortedFlats[i + 1].Number - sortedFlats[i].Number) == 1)
                        {
                            sortedFlats[i].SetSemitone();
                            i++;

                            if ((sortedFlats.Count - 1) == (i + 1)
                                && (sortedFlats[i + 1].Number - sortedFlats[i].Number) == 1)
                                sortedFlats[i + 1].SetSemitone();
                        }
                    }
                }

                trans.Commit();
            }
            catch (Exception exception)
            {
                trans.RollBack();
                message = exception.Message;
                return Result.Failed;
            }

            uiApp.ActiveUIDocument.RefreshActiveView();

            return Result.Succeeded;
        }

        private readonly StringBuilder _keyBuilder = new StringBuilder();
        private const char SeparatorSymbol = ' ';

        /// <summary>
        /// Составляем строковый ключ для группировки комнат по этажу, секции и типу квартиры
        /// </summary>
        /// <param name="room">Комната</param>
        /// <returns>Ключ для группировки</returns>
        private string GetRoomGroupingKey(Element room)
        {
            _keyBuilder.Clear();

            _keyBuilder.Append(room.GetParameters(LevelParam).LastOrDefault()?.AsString())
                .Append(SeparatorSymbol)
                .Append(room.LookupParameter(SectorParam)?.AsString())
                .Append(SeparatorSymbol)
                .Append(room.LookupParameter(FlatTypeParam)?.AsString());

            return _keyBuilder.ToString();
        }

        /// <summary>
        /// Определяем номер квартиры для комнаты
        /// </summary>
        /// <param name="room">Комната</param>
        /// <returns>Номер квартиры</returns>
        private int GetFlatNumber(Element room)
        {
            var sFlatNumber = room.LookupParameter(FlatParam)?.AsString().Split(SeparatorSymbol).LastOrDefault();
            int.TryParse(sFlatNumber, out var number);

            return number;
        }
    }
}
