const target =
  process.env['services__webapi__https__0'] ||
  process.env['services__webapi__http__0'];

if (!target) {
  throw new Error(
    "L’adresse de l’API est absente. Démarrer le frontend depuis AppHost."
  );
}

module.exports = {
  '/api/**': {
    target,
    changeOrigin: true,

    // Proxy de développement uniquement :
    // accepte le certificat HTTPS local de l’API.
    secure: false,
  },
};