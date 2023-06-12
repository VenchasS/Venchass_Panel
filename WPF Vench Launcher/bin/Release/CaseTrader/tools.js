const SteamUser = require('steam-user');
const SteamTotp = require('steam-totp');

const errorHandler = (message, error) => {
  if (error) {
    console.log(message + "\n" + error);
    process.exit(-1);
  }
};
const createQuickInvite = (username, password, sharedSecret) => {
  return new Promise((resolve, reqject) => {
    const client = new SteamUser();
    let code;
    try {
      if(sharedSecret !== "0") {
        code = SteamTotp.generateAuthCode(sharedSecret);
      }
    } catch (err) {
      console.log("error while getting code", err);
    }
    client.logOn({
      accountName: username,
      password: password,
      twoFactorCode: code,
    });
  
    client.on('loggedOn', () => {
      console.log('Logged into Steam to create link');
      client.createQuickInviteLink(undefined, (err, response) => {
        if (err) {
          errorHandler('Ошибка при создании ссылки', err)
        } else {
          console.log("link: ", response)
          resolve(response);
          client.logOff();
        }
      });
    });
    setTimeout(() => {
      reqject("timeout");
    }, 120000);
  })
  
}


let accounts = [];
const wait = 30;
const redeemInviteAndAddFriend = (username, password, shared_secret) => {
  const mainClient = new SteamUser();
  let code = "";
  try {
    if(shared_secret !== "0") {
      code = SteamTotp.generateAuthCode(shared_secret);
    }
  } catch {}
  mainClient.logOn({
    accountName: username,
    password: password,
    twoFactorCode: code,
  });
  mainClient.on('friendRelationship', (steamID, relationship) => {
    if (relationship === SteamUser.EFriendRelationship.RequestRecipient) {
      mainClient.addFriend(steamID);
      console.log(`User with SteamID ${steamID} added to friends list`);
    }
  });
  mainClient.on('loggedOn', () => {
    console.log('Logged into Steam main');
    for(const [login, pass, sharedSecret] of accounts) {
      createQuickInvite(login,pass,sharedSecret)
      .then(res => {
        console.log('Invite link ', res);
        mainClient.redeemQuickInviteLink(res.token.invite_link, (err) => {
          if (err) {
            console.log('Error redeeming invite link', err)
          } else {
            console.log('Invite link redeemed successfully');
          }
        });
      })
      .catch(err => {
        console.log("error getting invite", err);
      })
      
    }
  });
}

const args = process.argv;
if (args.length <= 2) {
  process.exit(0);
} 
const login = args[2];
const password = args[3]
const shared_secret = args[4]
let i = 5;
while(args[i]) {
  accounts.push([args[i], args[i + 1],args[i + 2]])
  i += 3
}
console.log("send invites started")
redeemInviteAndAddFriend(login, password, shared_secret);