using System;

[Serializable]
public class CargoDetails
{
    public string itemNo;
    public string length;
    public string width;
    public string height;
    public string weight;
    public string volume;
    public string totalBox;

    public CargoDetails(string itemNo, string length, string width, string height, string weight, string volume, string totalBox)
    {
        this.itemNo = itemNo;
        this.length = length;
        this.width = width;
        this.height = height;
        this.weight = weight;
        this.volume = volume;
        this.totalBox = totalBox;
    }
}
