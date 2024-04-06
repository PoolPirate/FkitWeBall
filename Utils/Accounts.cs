using Solnet.Wallet;

namespace FkitWeBall.Utils;
public static class Accounts
{
    public static readonly PublicKey TREASURY_ADDRESS = new PublicKey("FTap9fv2GPpWGqrLj3o4c9nHH7p36ih7NbSWHnrkQYqa");
    public static readonly PublicKey[] BUSSES = new string[] {
        "9ShaCzHhQNvH8PLfGyrJbB8MeKHrDnuPMLnUDLJ2yMvz",
        "4Cq8685h9GwsaD5ppPsrtfcsk3fum8f9UP4SPpKSbj2B",
        "8L1vdGdvU3cPj9tsjJrKVUoBeXYvAzJYhExjTYHZT7h7",
        "JBdVURCrUiHp4kr7srYtXbB7B4CwurUt1Bfxrxw6EoRY",
        "DkmVBWJ4CLKb3pPHoSwYC2wRZXKKXLD2Ued5cGNpkWmr",
        "9uLpj2ZCMqN6Yo1vV6yTkP6dDiTTXmeM5K3915q5CHyh",
        "EpcfjBs8eQ4unSMdowxyTE8K3vVJ3XUnEr5BEWvSX7RB",
        "Ay5N9vKS2Tyo2M9u9TFt59N1XbxdW93C7UrFZW3h8sMC",
    }.Select(x => new PublicKey(x)).ToArray();

    public static readonly PublicKey PROGRAM_ID = new PublicKey("mineRHF5r6S7HyD9SppBfVMXMavDkJsxwGesEvxZr2A");
    public static readonly PublicKey SYSVARSLOTHASH = new PublicKey("SysvarS1otHashes111111111111111111111111111");
}
