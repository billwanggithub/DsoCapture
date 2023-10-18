namespace DSO
{
    //public class DsoData
    //{

    //    public byte[] Raw { get; set; } = new byte[1];  // raw data , including header
    //    public byte[] Data { get; set; } = new byte[1]; // waveform data without header
    //    public byte[] Header { get; set; } = new byte[1]; // header bytes
    //    public double[] XData { get; set; } = new double[1];
    //    public double[] YData { get; set; } = new double[1];
    //    public string? Ch { get; set; }
    //    public int BytesPerPoint { get; set; }
    //    public float VerticalGain { get; set; }
    //    public float VerticalOffset { get; set; }
    //    public float VerticalZero { get; set; } = 0; // for tektronix
    //    public float HorizontalInterval { get; set; }
    //    public double HorizontalOffset { get; set; }

    //    public static int FindIndex(byte[] bytes, string key)
    //    {
    //        int byte_count = bytes.Length;
    //        byte[] buffer = new byte[key.Length];
    //        int i;
    //        for (i = 0; i < byte_count; i++)
    //        {
    //            Buffer.BlockCopy(bytes, i, buffer, 0, 8);
    //            if (Encoding.UTF8.GetString(buffer) == key)
    //            {
    //                break;
    //            }
    //        }
    //        return i;
    //    }
    //}
}
