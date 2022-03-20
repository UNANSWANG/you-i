using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Sys : MonoBehaviour
{
    //获取时间戳
    public static long GetTimeStamp()
    {
        //现在的时间距1970年1月1日0点的时间戳
        TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        return Convert.ToInt64(ts.TotalSeconds);
    }
}
