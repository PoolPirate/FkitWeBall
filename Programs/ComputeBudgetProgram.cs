using Solnet.Programs.Abstract;
using Solnet.Programs.Utilities;
using Solnet.Rpc.Models;
using Solnet.Wallet;

namespace FkitWeBall.Programs
{
    public class ComputeBudgetProgram : BaseProgram
    {
        public static readonly PublicKey ProgramIdKey = new("ComputeBudget111111111111111111111111111111");

        /// <summary>
        /// The program's name.
        /// </summary>
        private const string ProgramName = "Compute Budget Program";

        public ComputeBudgetProgram(PublicKey programIdKey, string programName) : base(programIdKey, programName)
        {
        }

        public static TransactionInstruction SetComputeUnitLimit(
             ulong amount)
        {
            return new TransactionInstruction
            {
                ProgramId = ProgramIdKey.KeyBytes,
                Keys = new List<AccountMeta>(),
                Data = ComputeBudgetProgramData.SetComputeUnitLimit(amount)
            };
        }
        /// <summary>
        ///  Instruction to add fee in microlamports
        /// </summary> 
        /// <param name="source"></param>
        /// <param name="microlamports">The amount in microlamports.</param>
        /// <returns></returns>
        public static TransactionInstruction SetComputeUnitPrice(
            ulong microlamports)
        {
            var instruction = new TransactionInstruction
            {
                ProgramId = ProgramIdKey.KeyBytes,
                Keys = new List<AccountMeta>(),
                Data = ComputeBudgetProgramData.SetComputeUnitPrice(microlamports)
            };
            return instruction;
        }


    }
}

namespace FkitWeBall.Programs
{
    internal static class ComputeBudgetProgramData
    {
        internal static byte[] RequestUnits(byte method, ulong units, ulong additionalFee)
        {
            byte[] methodBuffer = new byte[17];

            methodBuffer.WriteU8(method, 0);
            methodBuffer.WriteU32((byte)units, 1);
            methodBuffer.WriteU32((byte)additionalFee, 8);

            return methodBuffer;
        }


        internal static byte[] SetComputeUnitLimit(byte method, ulong units)
        {
            byte[] methodBuffer = new byte[9];

            methodBuffer.WriteU8(method, 0);
            methodBuffer.WriteU32((uint)units, 1);
            return methodBuffer;
        }

        internal static byte[] SetComputeUnitPrice(byte method, ulong microLamports)
        {
            byte[] methodBuffer = new byte[9];

            methodBuffer.WriteU8(method, 0);
            methodBuffer.WriteU64(microLamports, 1);
            return methodBuffer;
        }


        internal static byte[] SetComputeUnitLimit(ulong units)
           => SetComputeUnitLimit(2, units);

        internal static byte[] SetComputeUnitPrice(ulong microLamports)
           => SetComputeUnitPrice(3, microLamports);
    }
}

namespace FkitWeBall.Programs
{
    internal static class ComputeBudgetProgramInstructions
    {
        internal static readonly Dictionary<Values, string> Names = new()
        {
            { Values.RequestUnits, "Request Units" },
            { Values.RequestHeapFrame, "Request Heap Frame" },
            { Values.SetComputeUnitLimit, "Set Compute Unit Limit" },
            { Values.SetComputeUnitPrice, "Set Compute Unit Price" },

        };


        internal enum Values : byte
        {

            RequestUnits = 1,


            RequestHeapFrame = 2,


            SetComputeUnitLimit = 3,


            SetComputeUnitPrice = 4,


        }
    }
}

namespace Solnet.Programs.ComputeBudget.Models
{
    public class SetComputeUnitLimitParams
    {
        public int Units { get; set; }
    }
}

namespace Solnet.Programs.ComputeBudget.Models
{
    public class SetComputeUnitPriceParams
    {
        public long MicroLamports { get; set; }
    }
}