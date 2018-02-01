# Windows-Service-Beacon
README
Very simple Windows Service that tries to discover all the IP Addresses and Mac Address, after that it saves the results in Event Viewer, code 10 in Application. Right now it also pings all addresses and return the RoundTripTime of the ICMP packtet. Data are saved in EventViewer. Binary compiled can be found as "SI_ARP1.exe".
THIS APP MIGHT BE USEFULL FOR YOUR SIEM.
To install this service, execute this command line:

# C:\Windows\Microsoft.NET\Framework64\v4.0.30319\installutil.exe .\SI_ARP1.exe

Have Fun!
