using log4net;

namespace ReHUD.Models;

public class DataPoints {
    private static readonly ILog logger = LogManager.GetLogger(typeof(DataPoints));

    private readonly double?[] points;
    private int? currentIndex = null;

    public DataPoints(int length) {
        points = new double?[length];
    }

    public DataPoints(double trackLength, int gap) : this((int) Math.Ceiling(trackLength / gap)) { }

    public DataPoints(double[] points) {
        this.points = new double?[points.Length];
        for (int i = 0; i < points.Length; i++) {
            this.points[i] = points[i];
        }
    }

    public int Size() {
        return points.Length;
    }

    public double[] GetNonNullPoints() {
        double[] nonNullPoints = new double[points.Length];

        for (int i = 0; i < points.Length; i++) {
            if (points[i] == null) {
                throw new InvalidOperationException("Data point not set");
            }

            nonNullPoints[i] = points[i]!.Value;
        }

        return nonNullPoints;
    }

    public bool IsIndexSet() {
        return currentIndex != null;
    }

    public void SetIndex(int index) {
        currentIndex = CastIndexToRange(index);
    }

    public int? GetIndex() {
        return currentIndex;
    }

    public double? GetDataPoint() {
        AssureIndex();

        return points[currentIndex!.Value];
    }

    public double? GetDataPoint(int index) {
        return points[CastIndexToRange(index)];
    }

    public double? GetDataPoint(double index) {
        int index1 = CastIndexToRange((int) Math.Floor(index));
        int index2 = CastIndexToRange((int) Math.Ceiling(index));

        double? time1 = points[index1];
        double? time2 = points[index2];

        if (time1 == null || time2 == null) {
            return null;
        }

        return time1 + (time2!.Value - time1!.Value) * (index - index1);
    }

    public void SetDataPoint(double? time) {
        AssureIndex();

        points[currentIndex!.Value] = time;
    }

    public void AddDataPoint(double? time) {
        AssureIndex();

        IncrementIndex();
        points[currentIndex!.Value] = time;
    }

    public int DoUntilIndex(int index, Action<int, double?> action) {
        AssureIndex();

        index = CastIndexToRange(index);

        int count = 0;
        while (currentIndex!.Value != index) {
            IncrementIndex();
            count++;
            action(count, points[currentIndex!.Value]);
        }

        return count;
    }

    public int FillDataGap(int newIndex, double newTime) {
        AssureIndex();

        newIndex = CastIndexToRange(newIndex);

        int lastIndex = currentIndex!.Value;
        double? lastTime = GetDataPoint();

        return DoUntilIndex(newIndex, (diff, _) => {
            if (lastTime == null) {
                SetDataPoint(newTime);
            } else {
                SetDataPoint(lastTime + (newTime - lastTime) * diff / (newIndex - lastIndex));
            }
        });
    }

    public double? CalculateTimeDifference(int index1, int index2) {
        double? time1 = GetDataPoint(index1);
        double? time2 = GetDataPoint(index2);

        if (time1 == null || time2 == null) {
            return null;
        }

        return Math.Abs(time2!.Value - time1!.Value);
    }

    public double? CalculateDelta(double index, double currentTime) {
        int indexLow = (int) Math.Floor(index);

        double? timeInLap;
        if (indexLow == points.Length - 1) {
            timeInLap = points[indexLow];
        } else {
            timeInLap = GetDataPoint(index);
        }

        if (points[0] == null || timeInLap == null) {
            return null;
        }

        return currentTime - (timeInLap - points[0]);
    }

    private void IncrementIndex() {
        AssureIndex();

        currentIndex = CastIndexToRange(currentIndex!.Value + 1);
    }

    private int CastIndexToRange(int index) {
        return Mod(index, points.Length);
    }

    /// <summary>
    /// Throws an exception if the index is not set.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    private void AssureIndex() {
        if (!IsIndexSet()) {
            throw new InvalidOperationException("Index not set");
        }
    }

    private static int Mod(int x, int m) {
        return (x%m + m)%m;
    }

    public DataPoints CloneAndSubtractFirst() {
        DataPoints clone = new(points.Length);

        double first = points[0] ?? 0;
        for (int i = 0; i < points.Length; i++) {
            clone.points[i] = points[i] - first;
        }

        return clone;
    }
}