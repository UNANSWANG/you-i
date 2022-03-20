using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System;

public class Conn : MonoBehaviour
{
    public const int BUFFER_SIZE = 131072;
    public Socket socket;
    public byte[] readBffer = new byte[BUFFER_SIZE];
    public bool isUse = false;
    public int bufCount = 0;//已使用字节数
                            //字节数组转化成的字节长度
    public Int32 msgLen = 0;
    //字节长度,用字节存储后面字节的长度处理粘包分包
    public byte[] lenByte = new byte[sizeof(Int32)];
    //最后一次的心跳时间
    public long lastTickTime = long.MinValue;
    //添加玩家
    //public Player player;

    public Conn()
    {
        readBffer = new byte[BUFFER_SIZE];
    }
    public void init(Socket socket)
    {
        //获得心跳戳
        lastTickTime = Sys.GetTimeStamp();
        this.socket = socket;
        isUse = true;
        bufCount = 0;
    }
    public int BuffRemain()//缓冲区剩余字节
    {
        return BUFFER_SIZE - bufCount;
    }
    public string GetAdress()
    {
        if (!isUse)
        {
            return "无法获取地址";
        }
        return socket.RemoteEndPoint.ToString();
    }
    public void Close()
    {
        if (!isUse)
        {
            return;
        }
        /*if (player != null)
        {
            player.Logout();
            return;
        }*/
        print("[断开连接]：" + GetAdress());
        socket.Shutdown(SocketShutdown.Both);
        socket.Close();
        isUse = false;
    }
    //发送协议
    public void Send(ProtocolBase protoco)
    {
        Serv.instance.Send(this, protoco);
    }
}
