using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public InputField host;
    public InputField port;
    public InputField protocolSelect;
    void Start()
    {
        host = gameObject.transform.Find("Host").GetComponent<InputField>();
        port = gameObject.transform.Find("Port").GetComponent<InputField>();
        protocolSelect = gameObject.transform.Find("Protocol").GetComponent<InputField>();
        string host1 =GetLocalIp();
        host.text = host1;
        int port1 = int.Parse(port.text);
        //print(host1+"  "+port1);
        #region(其他样例)
        //Console.WriteLine("开始同步执行");
        //Add1(10);
        //Add2(20);
        //Console.ReadLine();
        //Console.WriteLine("开始异步编程了");
        //AddDelegate addDel = Add2;
        ////AsyncCallback委托要用的方法必须是IAsyncResult参数，该参数存了回调方法所需参数为object对象。
        //AsyncCallback callBack = Add3;
        ////IASyncResult参数1-N由自定义委托AddDelegate决定，AddDelegate有N个参数，那么就有N个参数
        ////callback为AsyncCallback委托，可为null，最后一个参数表示回调函数的参数，该值被存在 re.AsyncState中（为object对象）
        //IAsyncResult result = addDel.BeginInvoke(20, callBack, 10);
        //Add1(10);
        ////委托.EndInvoke(result)相当于一个监视器，一直在监视异步委托执行完成，一旦完成，则获取到结果并赋值到re中，与此同时会异步调用回调函数(有回调的情况下)。
        //var re = addDel.EndInvoke(result);
        //Console.WriteLine("Add2执行结果=" + re);
        //Add1(1);
        //Add1(5);
        //Add1(10);
        //Console.ReadLine();
        #endregion
        //DataMgr dataMgr = new DataMgr();
        Serv serv = new Serv_Byte();
        //使用字节协议初始化
        //serv.proto = new ProtocolBytes();
        serv.proto = new ProtocolTexture();
        serv.StartOnline(host1, port1);//192.168.101.16
    }
    public string GetLocalIp()
    {
        ///获取本地的IP地址
        string AddressIP = string.Empty;
        foreach (IPAddress _IPAddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
        {

            if (_IPAddress.AddressFamily.ToString() == "InterNetwork")
            {

                AddressIP = _IPAddress.ToString();
            }
        }
        return AddressIP;
    }

}
