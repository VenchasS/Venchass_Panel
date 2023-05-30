const SteamUser = require("steam-user");
const SteamCommunity = require("steamcommunity");
const SteamTotp = require("steam-totp");
const TradeOfferManager = require("steam-tradeoffer-manager");
const args = process.argv;

if (args.length <= 2) {
  process.exit(0);
} 


const login = args[2];
const password = args[3]
const account = args[4].split(":");
const sendLastItem = args[6];
const tradeOfferLink = args[5];
const [shared_secret, identity_secret] = account;

const errorHandler = (message, error) => {
  if (error) {
    console.log(message + "\n" + error);
    process.exit(-1);
  }
};
const sendTrade = () => {
  const client = new SteamUser();
  const manager = new TradeOfferManager({
    steam: client,
    language: "en",
  });
  const community = new SteamCommunity();
  const logOnOptions = {
    accountName: login,
    password: password,
    twoFactorCode: SteamTotp.getAuthCode(shared_secret),
  };

  client.on("loggedOn", () => {
    console.log(`${login} logged into Steam`);
  });
  client.logOn(logOnOptions);

  
  client.on("webSession", (sessionID, cookies) => {
    manager.setCookies(cookies, (err) => {
      //errorHandler("Something went wrong while setting webSession", err);

      manager.getInventoryContents(730, 2, true, (err, csgoInventory) => {
        errorHandler("Something went wrong while getting inventory", err);
        if(csgoInventory.length === 0){
          console.log("Found 0 items to trade");
          process.exit(1);
        }
        const offer = manager.createOffer(tradeOfferLink);
        let itemsCount = 0;
        if (sendLastItem === "0") {
          csgoInventory.forEach((item) => {
            if (item.tags[0].name === "Container") {
              itemsCount++;
              offer.addMyItem(item);
            }
          });
        } else {
          itemsCount++;
          offer.addMyItem(csgoInventory[0]);
        }

        console.log("Found " + itemsCount + " CS:GO items");

        offer.send(function (err, status) {
          errorHandler("Something went wrong while sending trade offer", err);

          if (status === "pending") {
            console.log(`Offer #${offer.id} sent, but requires confirmation`);
            community.acceptConfirmationForObject(
              identity_secret,
              offer.id,
              function (err) {
                if (!err) {
                  console.log("Offer confirmed");
                  process.exit(1);
                }
                errorHandler(
                  "Something went wrong during trade confirmation",
                  err
                );
              }
            );
          }
        });
      });
    });
    community.setCookies(cookies);
  });
};
sendTrade();
