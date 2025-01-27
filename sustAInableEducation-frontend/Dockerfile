FROM node:lts-alpine

WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .
RUN npm run build
RUN npm prune

ENV PORT=3000
ENV NODE_TLS_REJECT_UNAUTHORIZED=0
CMD [ "node", ".output/server/index.mjs" ]
