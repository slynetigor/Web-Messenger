﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Messenger.Core.BLL.Interfaces;
using Messenger.Core.DAL.Infrastructure;
using Messenger.Core.DAL.Models;
using Messenger.WebAPI.ViewModels.Chat;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Hubs;
using MongoDB.Bson.IO;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace Messenger.WebAPI.Hubs
{
    [HubName("chat")]
    [Authorize]
    public class ChatHub : AbstractChatHub
    {
        public ChatHub(
            IRoomsManager roomsManager,
            IMapper mapper,
            IMessageManager messageManager)
            : base(roomsManager, mapper, messageManager)
        {

        }

        public object CreateRoom(string name)
        {
            var room = new Room { Name = name };
            room = this.RoomProcess(room);
            var rooms = this.Mapper.Map<IEnumerable<Room>, IEnumerable<RoomJson>>(this.RoomRepository.GetEntities());
            this.OnRoomsCountChange(rooms);
            var users = this.Mapper.Map<IEnumerable<User>, IEnumerable<UserJson>>(room.Users);
            return new
            {
                rooms = rooms,
                users = users,
                roomId = room.Id,
            };
        }

        public object EnterRoom(string id, int messageCount)
        {
            var room = this.RoomRepository.GetById(id);

            if (room == null)
            {
                return null;
            }

            room = this.RoomProcess(room);
            var users = this.Mapper.Map<IEnumerable<User>, IEnumerable<UserJson>>(room.Users);
            return new
            {
                roomId = id,
                users = users,
                messagesModel = this.GetMessage(room.Id, messageCount, 1)
            };

        }

        private object GetMessage(string roomId, int messageCount, int messagePage)
        {
            var model = this.MessageManager.Repository.GetMessages(roomId, messageCount, messagePage);
            var response = this.Mapper
                .Map<MessagesResponseModel, MessagesJsonResponseModel>(model);
            return new
            {
                messages = response.Messages,
                totalPages = response.TotalPages,
                totalMessages = response.TotalMessages
            };
        }

        private Room RoomProcess(Room room)
        {
            var user = this.UsersRepository
               .GetById(this.CurrentUser.Id);
            var currentRoom = user.Room;

            if (currentRoom?.Users is User[])
            {
                currentRoom.Users = new List<User>(currentRoom.Users);
            }

            if (room?.Users is User[])
            {
                room.Users = new List<User>(room.Users);
            }

            if (!string.IsNullOrEmpty(room.Id))
            {
                this.RoomsManager.EnterToRoom(user, room);
            }
            else
            {
                this.RoomsManager.CreateRoom(user, room);
            }

            if (currentRoom != null)
            {
                currentRoom = this.RoomRepository.GetById(currentRoom.Id);
                this.RemoveFromGroup(currentRoom.Id);
                this.OnUserCountChanged(currentRoom);
            }

            this.AddToGroup(room.Id);
            this.OnUserCountChanged(room);
            return room;
        }

        public void UserTyping(string roomId)
        {
            this.OnUserWriting(roomId);
        }

#pragma warning disable Nix08, Nix09
        public MessageJson SendMessage(string roomId, string text)
        {
            var room = new Room { Id = roomId };
            var message = new Message
            {
                Date = DateTime.Now,
                Room = room,
                User = this.CurrentUser,
                Text = text
            };
            var task = Task.Run(action: () =>
            {
                this.MessageManager.SendMessage(message);
            });
            var messageJson = this.Mapper.Map<Message, MessageJson>(message);
            messageJson.UserName = this.CurrentUser.UserName;
            this.OnGetMessage(room, messageJson);
            return messageJson;
        }
    }
}