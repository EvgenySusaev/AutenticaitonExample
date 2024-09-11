const jwt = require('jsonwebtoken');

const payload = {
    user: {
        id: '6b967356-929d-45a0-91be-b29701027ece',
        roles: 'PLAYER'
    },
    isLoggedIn: true,
};

const TOKEN_SECRET = 'your-secret-key'; // Replace with your secret key

const token = jwt.sign(payload, TOKEN_SECRET, { expiresIn: '72h' });
console.log(token)
// Verify the token using your secret key
const secretKey = TOKEN_SECRET; // Replace with your actual secret key
jwt.verify(token, secretKey, (err, decoded) => {
    if (err) {
        // Token verification failed
        console.error('JWT verification failed:', err);
        // Handle the error
    } else {
        // Token is valid, and `decoded` contains the payload
        console.log('JWT verified. Payload:', decoded);
        // Proceed with authentication/authorization logic
    }
});


// eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyIjp7ImlkIjoiNmI5NjczNTYtOTI5ZC00NWEwLTkxYmUtYjI5NzAxMDI3ZWNlIiwicm9sZXMiOlsiR1VFU1QiXX0sImRldmljZSI6IkRldmljZSBBcm4iLCJpYXQiOjE2NzcwNjA2NTB9.ljB-91O8fzPNNtpp2gE6_cF6OMLIf72CkLn8aJjl4iw




