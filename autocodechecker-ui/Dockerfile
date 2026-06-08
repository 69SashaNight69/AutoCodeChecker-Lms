FROM node:20 AS build
WORKDIR /app

COPY package*.json ./
RUN npm install

COPY . .

EXPOSE 7002
ENV PORT=7002

CMD ["npm", "start"]