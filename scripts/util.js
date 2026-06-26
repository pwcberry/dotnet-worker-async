import crypto from "node:crypto";

function uuid(){
    const bytes = crypto.randomBytes(16).toString('hex');
    return `${bytes.substring(0, 8)}-${bytes.substring(8, 12)}-${bytes.substring(12, 16)}-${bytes.substring(16, 20)}-${bytes.substring(20)}`;
}

export {
    uuid,
};
