
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class Packet : IDisposable
{
    public int _seeker;
    public List<byte> Bytes_To_Send;
    public byte[] Received_Bytes;


    public Packet(int id) //write 
    {
        Bytes_To_Send = new List<byte>();
        Write(id);
    }

    public Packet(byte[] bytes) //read 
    {
        _seeker = 0;
        Received_Bytes = bytes.ToArray();
    }

    public void WriteFront(int val)
    {
        Bytes_To_Send.InsertRange(0, BitConverter.GetBytes(val));
    }

    public void Write(byte val)
    {
        Bytes_To_Send.Add(val);
    }

    public void Write(byte[] val)
    {
        Write(val.Length);
        Bytes_To_Send.AddRange(val);
    }
    public void Write(List<byte> val)
    {
        Write(val.Count);
        Bytes_To_Send.AddRange(val);
    }
    public void Write(int val)
    {
        Bytes_To_Send.AddRange(BitConverter.GetBytes(val));
    }
    public void Write(string val)
    {
        Write(val.Length);
        Bytes_To_Send.AddRange(Encoding.ASCII.GetBytes(val));
    }
    public void Write(float val)
    {
        Bytes_To_Send.AddRange(BitConverter.GetBytes(val));
    }
    public void Write(bool val)
    {
        Bytes_To_Send.AddRange(BitConverter.GetBytes(val));
    }
    public void Write(long val)
    {
        Bytes_To_Send.AddRange(BitConverter.GetBytes(val));
    }
    public void Write(short val)
    {
        Bytes_To_Send.AddRange(BitConverter.GetBytes(val));
    }
    public byte[] GetBytes(ref bool error)
    {
        int length = GetInt(ref error); if (error == true) return null;
        if (Received_Bytes.Length > _seeker)
        {
            if (_seeker + length - 1 >= Received_Bytes.Length)
            {
                error = true;
                return null;
            }
            byte[] val = Received_Bytes.Skip(_seeker).Take(length).ToArray();
            _seeker += length;
            return val;
        }
        else
        {
            error = true;
            return null;
        }
    }

    public int GetInt(ref bool error)
    {
        if (Received_Bytes.Length > _seeker)
        {
            if (_seeker + 3 >= Received_Bytes.Length)
            {
                error = true;
                return 0;
            }
            int val = BitConverter.ToInt32(Received_Bytes, _seeker);
            _seeker += 4;
            return val;
        }
        else
        {
            error = true;
            return 0;
        }
    }
    public string GetString(ref bool error)
    {
        try
        {
            int length = GetInt(ref error);
            if (error == true) return "";
            if (length < 0 || _seeker + length - 1 >= Received_Bytes.Length)
            {
                error = true;
                return "";
            }
            string val = Encoding.ASCII.GetString(Received_Bytes, _seeker, length);
            _seeker += length;
            return val;
        }
        catch
        {
            error = true;
            return "";
        }
    }
    public float GetFloat(ref bool error)
    {
        if (Received_Bytes.Length > _seeker)
        {
            if (_seeker + 3 >= Received_Bytes.Length)
            {
                error = true;
                return 0f;
            }
            float val = BitConverter.ToSingle(Received_Bytes, _seeker);
            _seeker += 4;
            return val;
        }
        else
        {
            error = true;
            return 0f;
        }
    }
    public bool GetBool(ref bool error)
    {
        if (Received_Bytes.Length > _seeker)
        {
            if (_seeker >= Received_Bytes.Length)
            {
                error = true;
                return false;
            }
            bool val = BitConverter.ToBoolean(Received_Bytes, _seeker);
            _seeker += 1;
            return val;
        }
        else
        {
            error = true;
            return false;
        }
    }

    public short GetShort(ref bool error)
    {
        if (Received_Bytes.Length > _seeker)
        {
            if (_seeker + 1 >= Received_Bytes.Length)
            {
                error = true;
                return 0;
            }
            short val = BitConverter.ToInt16(Received_Bytes, _seeker);
            _seeker += 2;
            return val;
        }
        else
        {
            error = true;
            return 0;
        }
    }
    public long GetLong(ref bool error)
    {
        if (Received_Bytes.Length > _seeker)
        {
            if (_seeker + 7 >= Received_Bytes.Length)
            {
                error = true;
                return 0;
            }
            long val = BitConverter.ToInt64(Received_Bytes, _seeker);
            _seeker += 8;
            return val;
        }
        else
        {
            error = true;
            return 0;
        }
    }

    public void _Finalize()
    {
        Bytes_To_Send.Add(233);
        Bytes_To_Send.Add(232);
        Bytes_To_Send.Add(231);
        Bytes_To_Send.Add(230);
    }
    public bool CheckFinalizers(ref bool error) // this is the end of a task - When we reach this area, if there are more tasks to be carried out in the packet, we can move on. 
    {
        if (_seeker + 3 >= Received_Bytes.Length)
        {
            error = true;
            return false;
        }
        if (Received_Bytes[_seeker++] == 233 && Received_Bytes[_seeker++] == 232 && Received_Bytes[_seeker++] == 231 && Received_Bytes[_seeker++] == 230)
        {
            return true;
        }
        else
        {
            error = true;
            return false;
        }
    }
    private bool disposed = false;

    protected virtual void Dispose(bool is_disposing)
    {
        if (disposed == false)
        {
            disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
