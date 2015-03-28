using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConcurrencyTestLib.Utils
{

    public enum API
    {
        ACCOUNT,
        WALLET,
        RTQ
    }

    public enum API_TRADECODES
    {
        ACNO_REG,
        ACCT_MODIFY_LEVEL,
        ACCT_MODIFYSTATUS,
        ACCT_UPDATE_PWD,
        ACCT_CREATE_RESET_PWD,
        ACCT_VALIDATE_PWD,
   
        ACCT_DEBIT_CREDIT,
        ACCT_DEBIT,
        ACCT_CREDIT,
        ACCT_FREEZE_BALANCE,
        ACCT_CREDIT_FRZ,
        ACCT_DEBIT_CREDIT_FRZ,
        ACCT_UNFREEZE_BALANCE,
        ACCT_UNFREEZE_DEBIT,
        ACCT_UNFRZ_DEBIT_FRZ_CREDIT,
        ACCT_UNFRZ_DEBIT_CREDIT,
        ACCT_REFUND,
        DATE_ACCT_SWITCH,

        ACCT_SIGQUERY_ACCTBAL,
        ACCT_BALANCE_HISTORY_QUERY,
        ACCT_BATCH_FQUERY,
        ORDER_HISTORY_QUERY,
        LIMIT_QUERY
    }
}
