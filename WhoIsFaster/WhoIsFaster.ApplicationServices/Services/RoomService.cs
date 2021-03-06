﻿using System;
using System.Threading.Tasks;
using WhoIsFaster.ApplicationServices.DTOs;
using WhoIsFaster.ApplicationServices.Exceptions;
using WhoIsFaster.ApplicationServices.Interfaces;
using WhoIsFaster.Domain.Entities;
using WhoIsFaster.Domain.Entities.RoomAggregate;
using WhoIsFaster.Domain.Interfaces;

namespace WhoIsFaster.ApplicationServices.Services
{
    public class RoomService : IRoomService
    {
        private readonly IUnitOfWork _unitOfWork;

        public RoomService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<RoomResponseDTO> CreateAndJoinPartyRoomAsync(string userName)
        {
            RegularUser regularUser = await _unitOfWork.RegularUserRepository.SecureGetByUserNameAsync(userName);
            Room room;
            var isNew = false;
            if (regularUser == null)
            {
                throw new WhoIsFasterException($"User with username {userName} doesn't exist.");
            }
            if (regularUser.IsInRoom)
            {
                room = await _unitOfWork.RoomRepository.GetJoinedRoomForUserName(userName);
                if (room != null)
                {
                    return new RoomResponseDTO(new RoomDTO(room), isNew);
                }
                throw new WhoIsFasterException($"User with username {userName} is already in a room.");
            }

            Text text = await _unitOfWork.TextRepository.GetRandomTextAsync();
            if (text == null)
            {
                throw new WhoIsFasterException($"There are no texts in the database.");
            }

            room = new Room(4, 2, text, 120, 5, RoomType.Party);
            isNew = true;
            room.PlayerJoin(regularUser);


            await _unitOfWork.RoomRepository.AddRoomAsync(room);
            await _unitOfWork.SaveChangesAsync();

            if (room != null)
            {
                regularUser.JoinRoom(room.Id);
                await _unitOfWork.SaveChangesAsync();
            }

            return new RoomResponseDTO(new RoomDTO(room), isNew);
        }

        public async Task StartRoom(int id)
        {
            Room room = await _unitOfWork.RoomRepository.GetByIdAsync(id);
            if (room == null)
            {
                throw new WhoIsFasterException($"Room with ID {id} doesn't exist.");
            }

            room.SetIsStarting();
            await _unitOfWork.SaveChangesAsync();
        }


        public async Task LeavePartyRoom(int id)
        {
            Room room = await _unitOfWork.RoomRepository.GetByIdAsync(id);
            if (room == null)
            {
                throw new WhoIsFasterException($"Room with ID {id} doesn't exist.");
            }

            if (!room.IsStarting && !room.HasStarted)
            {
                room.LeaveRoom();
                await _unitOfWork.SaveChangesAsync();
            }
        }

        public async Task<RoomResponseDTO> CreateAndJoinPracticeRoomAsync(string userName)
        {
            RegularUser regularUser = await _unitOfWork.RegularUserRepository.SecureGetByUserNameAsync(userName);
            Room room;
            var isNew = false;
            if (regularUser == null)
            {
                throw new WhoIsFasterException($"User with username {userName} doesn't exist.");
            }
            if (regularUser.IsInRoom)
            {
                room = await _unitOfWork.RoomRepository.GetJoinedRoomForUserName(userName);
                if (room != null)
                {
                    return new RoomResponseDTO(new RoomDTO(room), isNew);
                }
                throw new WhoIsFasterException($"User with username {userName} is already in a room.");
            }

            Text text = await _unitOfWork.TextRepository.GetRandomTextAsync();
            if (text == null)
            {
                throw new WhoIsFasterException($"There are no texts in the database.");
            }


            room = new Room(1, 1, text, 120, 5, RoomType.Practice);
            isNew = true;
            room.PlayerJoin(regularUser);

            await _unitOfWork.RoomRepository.AddRoomAsync(room);
                
            await _unitOfWork.SaveChangesAsync();

            regularUser.JoinRoom(room.Id);

            await _unitOfWork.SaveChangesAsync();

            return new RoomResponseDTO(new RoomDTO(room), isNew);

        }

        public async Task<RoomDTO> GetRoomByIdAsync(int id)
        {
            Room room = await _unitOfWork.RoomRepository.GetByIdAsync(id);
            if (room == null)
            {
                throw new WhoIsFasterException($"Room with ID {id} doesn't exist.");
            }

            return new RoomDTO(room);
        }

        public async Task<RoomDTO> GetRoomByUserNameAsync(string userName)
        {
            Room room = await _unitOfWork.RoomRepository.GetJoinedRoomForUserName(userName);
            if (room == null)
            {
                throw new WhoIsFasterException($"Room joined by user {userName} doesn't exist.");
            }

            return new RoomDTO(room);
        }

        public async Task<RoomResponseDTO> JoinOrCreateRoomAsync(string userName)
        {
            RegularUser regularUser = await _unitOfWork.RegularUserRepository.SecureGetByUserNameAsync(userName);
            bool isNew = false;
            Room room;
            if (regularUser == null)
            {
                throw new WhoIsFasterException($"User with username {userName} doesn't exist.");
            }
            if (regularUser.IsInRoom)
            {
                room = await _unitOfWork.RoomRepository.GetJoinedRoomForUserName(userName);
                if (room != null)
                {
                    return new RoomResponseDTO(new RoomDTO(room), isNew);
                }
                throw new WhoIsFasterException($"User with username {userName} is already in a room.");
            }

            room = await _unitOfWork.RoomRepository.GetRandomNotStartedJoinablePublicRoom();
            if (room == null)
            {
                Text text = await _unitOfWork.TextRepository.GetRandomTextAsync();
                if (text == null)
                {
                    throw new WhoIsFasterException($"There are no texts in the database.");
                }

                room = new Room(4, 2, text, 120, 5, RoomType.Public);
                await _unitOfWork.RoomRepository.AddRoomAsync(room);
                isNew = true;
            }

            room.PlayerJoin(regularUser);
            await _unitOfWork.SaveChangesAsync();

            if (room != null)
            {
                regularUser.JoinRoom(room.Id);
                await _unitOfWork.SaveChangesAsync();
            }

            return new RoomResponseDTO(new RoomDTO(room), isNew);

        }

        public async Task<RoomResponseDTO> JoinPartyRoomAsync(string userName, int roomId)
        {
            Room room = await _unitOfWork.RoomRepository.GetByIdAsync(roomId);
            if (room == null)
            {
                throw new WhoIsFasterException($"Room with ID {roomId} doesn't exist.");
            }

            RegularUser regularUser = await _unitOfWork.RegularUserRepository.SecureGetByUserNameAsync(userName);
            if (regularUser == null)
            {
                throw new WhoIsFasterException($"User with username {userName} doesn't exist.");
            }
            if (room.RoomType != RoomType.Party)
            {
                return new RoomResponseDTO(new RoomDTO(room), false);
            }
            if (regularUser.IsInRoom)
            {
                if (regularUser.CurrentRoomId == roomId)
                {
                    return new RoomResponseDTO(new RoomDTO(room), false);
                }
                throw new WhoIsFasterException($"User with username {userName} is already in a room.");
            }


            room.PlayerJoin(regularUser);

            regularUser.JoinRoom(room.Id);
            await _unitOfWork.SaveChangesAsync();

            return new RoomResponseDTO(new RoomDTO(room), false);
        }

    }
}
