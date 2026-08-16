namespace Warehouse.Application.Services
{
    public class RouteOptimizerService : IRouteOptimizerService
    {
        public List<T> OptimizeRoute<T>(IEnumerable<T> items, Func<T, string?> locationBarcodeSelector)
        {
            var withCoordinates = new List<(T Item, LocationCoordinate Coordinate)>();
            var unroutable = new List<T>();

            foreach (var item in items)
            {
                if (LocationCoordinate.TryParse(locationBarcodeSelector(item), out var coordinate))
                    withCoordinates.Add((item, coordinate!));
                else
                    unroutable.Add(item);
            }

            var visitIndexByAisle = BuildAisleVisitOrder(withCoordinates.Select(x => x.Coordinate));

            var sorted = withCoordinates
                .OrderBy(x => x.Coordinate.Warehouse)
                .ThenBy(x => x.Coordinate.Sector)
                .ThenBy(x => x.Coordinate.Floor)
                // Aisles are walked from the highest row numbers down to the lowest.
                .ThenByDescending(x => x.Coordinate.Aisle)
                // Serpentine: the first aisle visited in a group sorts sections ascending
                // (walk up), the next sorts descending (walk down), and so on. Negating
                // the section number is a cheap way to flip an ascending sort to descending.
                .ThenBy(x => IsAscendingVisit(visitIndexByAisle, x.Coordinate) ? x.Coordinate.Section : -x.Coordinate.Section)
                .ThenBy(x => x.Coordinate.Row)
                .ThenBy(x => x.Coordinate.Level)
                .ThenBy(x => x.Coordinate.Position)
                .Select(x => x.Item);

            return sorted.Concat(unroutable).ToList();
        }

        // Resets per (Warehouse, Sector, Floor): within each such group, the distinct
        // aisles present are ranked in descending order (0 = first visited, 1 = second, ...).
        // Whether that rank is even or odd decides the section sort direction for that aisle.
        private static Dictionary<(char Warehouse, char Sector, char Floor, int Aisle), int> BuildAisleVisitOrder(
            IEnumerable<LocationCoordinate> coordinates)
        {
            return coordinates
                .Select(c => (c.Warehouse, c.Sector, c.Floor, c.Aisle))
                .Distinct()
                .GroupBy(k => (k.Warehouse, k.Sector, k.Floor))
                .SelectMany(group => group
                    .OrderByDescending(k => k.Aisle)
                    .Select((k, index) => (k, index)))
                .ToDictionary(x => x.k, x => x.index);
        }

        private static bool IsAscendingVisit(
            Dictionary<(char Warehouse, char Sector, char Floor, int Aisle), int> visitIndexByAisle,
            LocationCoordinate coordinate)
        {
            var key = (coordinate.Warehouse, coordinate.Sector, coordinate.Floor, coordinate.Aisle);
            return visitIndexByAisle[key] % 2 == 0;
        }

        // Parses the fixed 11-character location barcode format:
        // [0] Warehouse [1] Sector [2] Floor [3,4] Row [5,6,7] Section [8,9] Level [10] Position
        private sealed class LocationCoordinate
        {
            public char Warehouse { get; }
            public char Sector { get; }
            public char Floor { get; }
            public int Row { get; }
            public int Section { get; }
            public int Level { get; }
            public char Position { get; }

            // Two consecutive rows face each other across one aisle (e.g. rows 40 and 41
            // both belong to aisle 20) — integer division collapses the pair onto it.
            public int Aisle => Row / 2;

            private LocationCoordinate(char warehouse, char sector, char floor, int row, int section, int level, char position)
            {
                Warehouse = warehouse;
                Sector = sector;
                Floor = floor;
                Row = row;
                Section = section;
                Level = level;
                Position = position;
            }

            public static bool TryParse(string? barcode, out LocationCoordinate? coordinate)
            {
                coordinate = null;

                if (string.IsNullOrEmpty(barcode) || barcode.Length != 11)
                    return false;

                if (!int.TryParse(barcode.AsSpan(3, 2), out var row)) return false;
                if (!int.TryParse(barcode.AsSpan(5, 3), out var section)) return false;
                if (!int.TryParse(barcode.AsSpan(8, 2), out var level)) return false;

                coordinate = new LocationCoordinate(barcode[0], barcode[1], barcode[2], row, section, level, barcode[10]);
                return true;
            }
        }
    }
}
