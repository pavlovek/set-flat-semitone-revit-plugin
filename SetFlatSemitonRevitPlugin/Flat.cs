using System.Collections.Generic;
using System.Text;
using Autodesk.Revit.DB;

namespace SetFlatSemitoneRevitPlugin
{
    /// <summary>
    /// Помещения составляющие квартиру с порядковым заданным номером
    /// </summary>
    internal class Flat
    {
        private const string ZoneIdParam = "ROM_Расчетная_подзона_ID";
        private const string SubZoneIdParam = "ROM_Подзона_Index";

        public Flat(int flatNumber, List<Element> rooms)
        {
            Number = flatNumber;
            Rooms = rooms;
        }

        /// <summary>
        /// Порядковый номер квартиры
        /// </summary>
        public int Number { get; }

        /// <summary>
        /// Элементы описывающие комнаты квартиры
        /// </summary>
        public List<Element> Rooms { get; }

        /// <summary>
        /// Индикатор подкрашивания квартиры в полутон
        /// </summary>
        public bool IsSemitone { get; private set; }

        private readonly StringBuilder _subZoneIdBuilder = new StringBuilder();
        private const string SemitoneSuffix = ".Полутон";

        /// <summary>
        /// Подкрасить помещения квартиры в полутон
        /// </summary>
        public void SetSemitone()
        {
            var subZoneId = string.Empty; 
            foreach (var room in Rooms)
            {
                if (string.IsNullOrEmpty(subZoneId))
                {
                    var zoneId = room.LookupParameter(ZoneIdParam)?.AsString();
                    subZoneId = _subZoneIdBuilder.Clear()
                        .Append(zoneId)
                        .Append(SemitoneSuffix).ToString();
                }

                room.LookupParameter(SubZoneIdParam)?.Set(subZoneId);
            }

            IsSemitone = true;
        }
    }
}
