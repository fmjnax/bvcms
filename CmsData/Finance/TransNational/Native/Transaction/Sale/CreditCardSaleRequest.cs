﻿using CmsData.Finance.TransNational.Native.Core;

namespace CmsData.Finance.TransNational.Native.Transaction.Sale
{
    internal class CreditCardSaleRequest : TransactRequest
    {
        public CreditCardSaleRequest(string userName, string password, CreditCard creditCard, decimal amount) 
            : base(userName, password)
        {
            Data["type"] = "sale";
            Data["payment"] = "creditcard";
            creditCard.SetCreditCardData(Data);
            Data["amount"] = amount.ToString("0.00");
        }

        public CreditCardSaleRequest(string userName, string password, CreditCard creditCard, decimal amount, string orderId)
            : this(userName, password, creditCard, amount)
        {
            Data["orderid"] = orderId;
        }

        public CreditCardSaleRequest(string userName, string password, CreditCard creditCard, decimal amount, string orderId, string orderDescription)
            : this(userName, password, creditCard, amount, orderId)
        {
            Data["orderdescription"] = orderDescription;
        }
    }
}
