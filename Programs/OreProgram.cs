using FkitWeBall.Utils;
using Solnet.Rpc.Models;
using Solnet.Wallet;
using System.Text;

namespace FkitWeBall.Programs;
public static class OreProgram
{
    private static readonly byte[] _proof = Encoding.UTF8.GetBytes("proof");

    public static TransactionInstruction Mine(PublicKey signer, PublicKey bus, ReadOnlySpan<byte> hash, ulong nonce)
    {
        var data = new byte[41];
        data[0] = 2; //Mine Instruction Id
        hash.CopyTo(data.AsSpan()[1..]);
        BitConverter.TryWriteBytes(data.AsSpan()[33..], nonce);

        PublicKey.TryFindProgramAddress([_proof, signer], Accounts.PROGRAM_ID, out var proof, out _);

        List<AccountMeta> keys = [
            AccountMeta.Writable(signer, true),
            AccountMeta.Writable(bus, false),
            AccountMeta.Writable(proof, false),
            AccountMeta.Writable(Accounts.TREASURY_ADDRESS, false),
            AccountMeta.ReadOnly(Accounts.SYSVARSLOTHASH, false),
        ];

        return new TransactionInstruction()
        {
            ProgramId = Accounts.PROGRAM_ID,
            Keys = keys,
            Data = data
        };
    }
}
