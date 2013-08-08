. .\TestSupport.ps1


Cleanup

"C=5, no sagas"
RunTest -transport "msmq" -messagemode "sagamessages" -numMessages 5 -persistence nhibernate -concurrency 10

"C=5, existing sagas"
#RunTest -transport "msmq" -messagemode "sagamessages" -numMessages 10 -persistence nhibernate -concurrency 10
