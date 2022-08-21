using Unity.Networking.Transport;

public class NetMessage
{
    public OpCode Code {set; get;}

    public virtual void Serialize(ref DataStreamWriter writer){
        writer.WriteByte((byte)Code);
    }

    public virtual void Deserialize(DataStreamReader reader){

    }
    
    public virtual void ReceivedOnclient(){

    }

    public virtual void ReceivedOnServer(NetworkConnection cnn){

    }
}
