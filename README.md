# Stock_Transactions_Match
Match sells to buys of stock transaction to compute capital gains

Build on VS 2019 and C#.

Put the 2 input files: buy.csv and sell.csv in the same folder as the code

The input files should have the following columns:

 - Stock Name
 - Trading Date
 - Buy Qty
 - Buy Rate
 - Buy Amount
 - Buy Charges (Brokerage and other statutory charges).
 
 

The output will be 2 files: 
1. cg.csv which holds the matched transaction with the following columns:
 - Name
 - Qty
 - BuyDate
 - BuyRate
 - BuyAmt
 - BuyCharges
 - SellDate
 - SellRate
 - SellAmt
 - SellCharges
 
 This helpd is computing gains / losses for each transaction.
 
2. buy_out.csv which holds the stocs left in your portfolio with the following columns:
 - Name
 - Date
 - Qty
 - Rate
 - Amt
 - Charges
