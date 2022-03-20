using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Timers;
using System.Linq;
using System.Reflection;

namespace Connect2
{
    abstract class Serv
    {
        public Socket listener;
        public Conn[] conns;//客户端数组
        public int maxCount = 50;
        //主定时器
        public Timer timer = new Timer(1000);
        //心跳时间
        public long heartBeatTime = 180;
        //静态变量
        public static Serv instance;
        //协议
        public ProtocolBase proto;

        //消息处理
        public HandleConnMsg handleConnMsg = new HandleConnMsg();
        public HandlePlayerMsg handlePlayerMsg = new HandlePlayerMsg();
        public HandlePlayerEvent handlePlayerEvent = new HandlePlayerEvent();

        public Serv()
        {
            instance = this;
        }

        //找到一个新的没有客户端的索引
        public int newIndex()
        {
            if (conns == null)
            {
                return -1;
            }
            for (int i = 0; i < maxCount; i++)
            {
                if (conns[i]==null)
                {
                    conns[i] = new Conn();
                    return i;
                }
                else if (!conns[i].isUse)
                {
                    return i;
                }
            }
            return -1;
        }
        //开启服务器
        virtual public void Start(string host,int port)
        {
            //定时器的配置
            timer.Elapsed +=new ElapsedEventHandler(HandleMainTimer);
            timer.AutoReset = false;//只让其启动一次
            timer.Enabled = true;

            conns = new Conn[maxCount];//创空间
            for (int i = 0; i < maxCount; i++)
            {
                conns[i] = new Conn();//初始化
            }
            //服务器的套接字
            listener = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);
            IPAddress ipAdr = IPAddress.Parse(host);
            IPEndPoint ipEP = new IPEndPoint(ipAdr,port);
            listener.Bind(ipEP);
            listener.Listen(maxCount);
            //接收
            listener.BeginAccept(AcceptCb,null);//调用AcceptCb回调函数
            Console.WriteLine("[服务器启动成功]");
        }
        virtual public void AcceptCb(IAsyncResult ar)
        {
            try
            {
                //接收客户端
                Socket socket = listener.EndAccept(ar);
                int index = newIndex();
                if (index<0)
                {
                    socket.Close();
                    Console.WriteLine("[警告]连接已满");
                }
                else
                {
                    Conn conn = conns[index];
                    conn.init(socket);
                    string host = conn.GetAdress();
                    Console.WriteLine("客户端连接:["+host+"] conn池ID:"+index);
                    conn.socket.BeginReceive(conn.readBffer,conn.bufCount,conn.BuffRemain(),SocketFlags.None,ReciveCb,conn);//接收的同时调用ReciveCb回调函数
                }
                listener.BeginAccept(AcceptCb,null);//再次调用AcceprCb回调函数
            }
            catch (Exception e)
            {
                Console.WriteLine("AccpetCb 失败:"+e.Message);
            }
        }
        virtual public void ReciveCb(IAsyncResult ar)
        {
            Conn conn = (Conn)ar.AsyncState;//这个AsyncState就是上面那个BeginRecive函数里面最后一个参数
            lock (conn)
            {
                try
                {
                    int count = conn.socket.EndReceive(ar);//返回接收的字节数
                    //没有信息就关闭
                    if (count<=0)
                    {
                        Console.WriteLine("收到["+conn.GetAdress()+"] 断开连接");
                        conn.Close();
                        return;
                    }
                    conn.bufCount += count;
                    ProcessData(conn);
                    #region(不使用协议的数据处理)
                    /*//数据处理
                    string str = System.Text.Encoding.UTF8.GetString(conn.readBffer,0,count);
                    Console.WriteLine("收到[" + conn.GetAdress() + "] 数据"+str);
                    str = conn.GetAdress() + ":" + str;
                    byte[] wrBuffer = System.Text.Encoding.Default.GetBytes(str);
                    for (int i = 0; i < conns.Length; i++)
                    {
                        if (conns[i]==null||!conns[i].isUse)//没有连接或者未被使用就跳过
                        {
                            continue;
                        }
                        Console.WriteLine("将消息传播给+",conns[i].GetAdress());
                        conns[i].socket.Send(wrBuffer);
                    }*/
                    #endregion
                    //继续接收
                    conn.socket.BeginReceive(conn.readBffer,conn.bufCount,conn.BuffRemain(),SocketFlags.None,ReciveCb,conn);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Recive失败"+e.Message);
                }
            }
        }
        //信息处理
        virtual public void ProcessData(Conn conn)
        {
            //小于字节长度
            if (conn.bufCount<sizeof(Int32))
            {
                return;
            }
            Console.WriteLine("接收到了 " + conn.bufCount+" 个字节");
            Array.Copy(conn.readBffer,conn.lenByte,sizeof(Int32));
            conn.msgLen = BitConverter.ToInt32(conn.lenByte,0);
            //小于最小要求长度则返回表示未接收完全
            if (conn.bufCount < conn.msgLen + sizeof(Int32))
            { 
                return; 
            }
            #region(不使用协议的消息处理)
            //处理消息
            /*string str = Encoding.UTF8.GetString(conn.readBffer,sizeof(Int32),conn.msgLen);//不使用协议的获取字符串
            Console.WriteLine("收到消息 [" + conn.GetAdress() + "] " + str);
            Send(conn, str);*/
            #endregion

            //这里接收信息有个细节，因为之前发送回来的信息又被加了一次长度，相当于要把他所有的信息接收完了
            //才算接收成功，然后再把前面的sizeof(Int32)去掉，剩下的就是带长度的信息了
            ProtocolBase protocol = proto.Decode(conn.readBffer, sizeof(Int32), conn.msgLen);
            //HandleMsg(conn, protocol);
            Send(conn, protocol);

            //清除已处理的消息
            int count = conn.bufCount - conn.msgLen - sizeof(Int32);
            Array.Copy(conn.readBffer, sizeof(Int32) + conn.msgLen, conn.readBffer, 0, count); 
            conn.bufCount = count; 
            //如果还有多余信息就继续处理
            if (conn.bufCount > 0) 
            {
                ProcessData(conn);
            }
        }
        //正常使用粘包粘包发送信息
        /*public void Send(Conn conn,string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            byte[] length = BitConverter.GetBytes(bytes.Length);
            //将长度和信息粘合在一起
            byte[] sendbuff = length.Concat(bytes).ToArray();
            try 
            { 
                conn.socket.BeginSend(sendbuff, 0, sendbuff.Length, SocketFlags.None, null, null);
            } 
            catch (Exception e) 
            { 
                Console.WriteLine("[发送消息 ]" + conn.GetAdress() + " : " + e.Message);
            }
        }*/
        //处理协议
        virtual public void HandleMsg(Conn conn,ProtocolBase pro)
        {
            string name = pro.GetName();
            //拿到方法名字
            string methodName = "Msg" + name;
            #region(不使用协议的方法)
            //Console.WriteLine("[收到协议] "+name);
            ////处理心跳
            ////心跳协议，如果为HeatBeat则改变上次心跳时间
            //if (name == "HeatBeat")
            //{
            //    conn.lastTickTime = Sys.GetTimeStamp();
            //    Console.WriteLine("[更新心跳时间 ]" + conn.GetAdress());
            //}
            #endregion
            //使用协议的方法
            //连接协议分发
            if (conn.player == null || name == "HeatBeat" || name == "Logout")
            {
                //通过反射来获取类的方法
                MethodInfo mm = handleConnMsg.GetType().GetMethod(methodName);
                //如果没有反射到则抛出错误
                if (mm==null)
                {
                    string str = "[警告 ](连接协议)HandleMsg没有处理连接方法 ";
                    Console.WriteLine(str + methodName);
                    return;
                }
                object[] obj = new object[] { conn,pro};
                Console.WriteLine("[处理连接消息 ]" + conn.GetAdress() + " :" + name);
                //第二个是传给mm获取到的方法的参数
                mm.Invoke(handleConnMsg,obj);
            }
            //角色协议分发
            else
            {
                Console.WriteLine("play");
                //通过反射来获取类的方法
                MethodInfo mm = handlePlayerMsg.GetType().GetMethod(methodName);
                //如果没有反射到则抛出错误
                if (mm == null)
                {
                    string str = "[警告 ](角色协议)HandleMsg没有处理连接方法 ";
                    Console.WriteLine(str + methodName);
                    return;
                }
                object[] obj = new object[] { conn.player, pro };
                Console.WriteLine("[处理玩家消息 ]" + conn.player.id + " :" + name);
                mm.Invoke(handlePlayerMsg, obj);
            }
            //Send(conn,pro);
        }
        //使用协议发送信息
        virtual public void Send(Conn conn,ProtocolBase pro)
        {
            //Console.WriteLine("bbb");
            byte[] bytes = pro.Encode();
            byte[] length = BitConverter.GetBytes(bytes.Length);
            //将长度和信息粘合在一起
            byte[] sendbuff = length.Concat(bytes).ToArray();
            Console.WriteLine("发送的字节长度 " +sendbuff.Length);
            try
            {
                conn.socket.BeginSend(sendbuff, 0, sendbuff.Length, SocketFlags.None, null, null);
            }
            catch (Exception e)
            {
                Console.WriteLine("[发送消息 ]" + conn.GetAdress() + " : " + e.Message);
            }
        }

        //主定时器
        virtual public void HandleMainTimer(object sender,ElapsedEventArgs e)
        {
            HeartBeat();
            //在回调函数HandleMainTimer中再次调用Start方法，使定时器不断执行
            timer.Start();
        }

        virtual public void HeartBeat()
        {
            //Console.WriteLine("主定时器执行");
            long timeNow = Sys.GetTimeStamp();
            for (int i = 0; i < conns.Length; i++)
            {
                //该池子为空或者未被使用
                if (conns[i]==null||!conns[i].isUse)
                {
                    continue;
                }
                if (conns[i].lastTickTime<timeNow-heartBeatTime)
                {
                    Console.WriteLine("[心跳引起断开连接]"+conns[i].GetAdress()+"  该连接为"+i+"个池子");
                    //加锁防止多线程占线关闭此套接字
                    lock (conns[i])
                    {
                        Console.WriteLine("关闭 " + i + "套接字    ");
                        conns[i].Close();
                    }
                }
            }
        }
        //广播
        public void Broadcast(ProtocolBase pro)
        {
            for (int i = 0; i < conns.Length; i++)
            {
                if (conns[i]==null||!conns[i].isUse)
                {
                    continue;
                }
                Send(conns[i],pro);
            }
        }

        //关闭
        public void Close()
        {
            for (int i = 0; i < conns.Length; i++)
            {
                Conn conn = conns[i];
                if (conn==null||!conn.isUse)
                {
                    continue;
                }
                lock (conn)
                {
                    conn.Close();
                }
            }
        }

        //打印
        public void Print()
        {
            Console.WriteLine("=== 服务器登录信息 ===");
            for (int i = 0; i < conns.Length; i++)
            {
                if (conns[i]==null||!conns[i].isUse)
                {
                    continue;
                }
                string str = "连接[ " + conns[i].GetAdress() + " ]";
                if (conns[i].player!=null)
                {
                    str += "玩家id " + conns[i].player.id;
                }
                Console.WriteLine(str);
            }
        }
    }
}
