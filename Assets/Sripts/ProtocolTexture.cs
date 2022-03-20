using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

public class ProtocolTexture : ProtocolBase
{
    public int x;
    public int y;
    public int z;
    public byte[] bytes;
    public byte[] texBytes;
    public ProtocolBytes pb;

    public override ProtocolBase Decode(byte[] readBuffer, int start, int len)
    {
        ProtocolTexture protocol = new ProtocolTexture();
        protocol.bytes = new byte[len];
        Array.Copy(readBuffer, start, protocol.bytes, 0, len);
        protocol.GetTex();
        return protocol;
    }

    public void GetTex()
    {
        int start = 0;
        GetString(start, ref start);
        if (bytes == null || bytes.Length < start + sizeof(Int32))
        {
            return;
        }
        Int32 strlen = BitConverter.ToInt32(bytes, start);
        if (bytes.Length < start + strlen + sizeof(Int32))
        {
            return;
        }
        texBytes = new byte[bytes.Length - start];
        Array.Copy(bytes, start, texBytes, 0, bytes.Length - start);
    }
    public void AddTex(byte[] tex)
    {
        Int32 len = tex.Length;
        byte[] lenByte = BitConverter.GetBytes(len);
        if (bytes == null)
        {
            bytes = lenByte.Concat(tex).ToArray();
        }
        else
        {
            bytes = bytes.Concat(lenByte).Concat(tex).ToArray();
        }
    }
    public override byte[] Encode()
    {
        return bytes;
    }

    public override string GetName()
    {
        return GetString(0);
    }

    public override string GetDesc()
    {
        string str = "";
        if (bytes == null)
        {
            return str;
        }
        for (int i = 0; i < bytes.Length; i++)
        {
            int t = (int)bytes[i];
            str += t.ToString() + " ";
        }
        return str;
    }

    public void AddString(string str)
    {
        Int32 len = str.Length;
        byte[] lenByte = BitConverter.GetBytes(len);
        byte[] strByte = Encoding.UTF8.GetBytes(str);
        if (bytes == null)
        {
            bytes = lenByte.Concat(strByte).ToArray();
        }
        else
        {
            bytes = bytes.Concat(lenByte).Concat(strByte).ToArray();
        }
    }

    //从start处开始读取字节数组
    public string GetString(int start, ref int end)
    {
        if (bytes == null || bytes.Length < start + sizeof(Int32))
        {
            return "";
        }
        Int32 strlen = BitConverter.ToInt32(bytes, start);
        if (bytes.Length < start + strlen + sizeof(Int32))
        {
            return "";
        }
        string str = Encoding.UTF8.GetString(bytes, start + sizeof(Int32), strlen);
        end = strlen + start + sizeof(Int32);
        return str;
    }

    public string GetString(int start)
    {
        int end = 0;
        return GetString(start, ref end);
    }

    public void AddInt(int num)
    {
        byte[] numBytes = BitConverter.GetBytes(num);
        if (bytes == null)
        {
            bytes = numBytes;
        }
        else
        {
            bytes = bytes.Concat(numBytes).ToArray();
        }
    }

    public int GetInt(int start, ref int end)
    {
        if (bytes == null || bytes.Length < start + sizeof(Int32))
        {
            return 0;
        }
        end = start + sizeof(Int32);
        return BitConverter.ToInt32(bytes, start);
    }

    public int GetInt(int start)
    {
        int end = 0;
        return GetInt(start, ref end);
    }

    public void AddFloat(float num)
    {
        byte[] numBytes = BitConverter.GetBytes(num);
        if (bytes == null)
        {
            bytes = numBytes;
        }
        else
        {
            bytes = bytes.Concat(numBytes).ToArray();
        }
    }

    public float GetFloat(int start, ref int end)
    {
        if (bytes == null || bytes.Length < start + sizeof(float))
        {
            return 0;
        }
        end = start + sizeof(float);
        return BitConverter.ToInt32(bytes, start);
    }

    public float GetFloat(int start)
    {
        int end = 0;
        return GetFloat(start, ref end);
    }
}
